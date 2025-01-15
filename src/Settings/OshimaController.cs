using System.Collections;
using Milimoe.FunGame.Core.Api.Utility;
using Milimoe.FunGame.Core.Controller;
using Milimoe.FunGame.Core.Library.Common.Network;
using Milimoe.FunGame.Core.Library.Constant;
using Milimoe.FunGame.Core.Model;
using Milimoe.OneBot.Model.Other;

namespace Milimoe.RainBOT.Settings
{
    public class OshimaController : RunTimeController
    {
        public static OshimaController Instance { get; set; } = new();
        public static FunGameConfig Config { get; set; } = new();
        public static int CurrentRetryTimes { get; set; } = -1;
        public static int MaxRetryTimes => 30;
        public const string ServerName = "oshima.fungame.anonymous";

        public override bool BeforeConnect(ref string addr, ref int port, ArrayList args)
        {
            if (Config.FunGame_isRetrying)
            {
                Console.WriteLine("正在连接服务器，请耐心等待。");
                return false;
            }
            string[] strings = ["oshima.fungame.fastauto"];
            args.Add(strings);
            args.Add(false);
            if (!Config.FunGame_isConnected)
            {
                CurrentRetryTimes++;
                if (CurrentRetryTimes == 0) Console.WriteLine("开始连接服务器...");
                else Console.WriteLine("第" + CurrentRetryTimes + "次重试连接服务器...");
                // 超过重连次数上限
                if (CurrentRetryTimes + 1 > MaxRetryTimes)
                {
                    Config.FunGame_isAutoRetry = false;
                    Console.WriteLine("无法连接至服务器。");
                    _ = Bot.SendFriendMessage(GeneralSettings.Master, "websocket", "重连服务器失败，重试次数已过多。");
                    return false;
                }
                Config.FunGame_isRetrying = true;
                return true;
            }
            else
            {
                Console.WriteLine("已连接至服务器，请勿重复连接。");
                return false;
            }
        }

        public override void AfterConnect(ArrayList ConnectArgs)
        {
            Config.FunGame_isRetrying = false;
            string msg = ConnectArgs[1]?.ToString() ?? "";
            string serverName = ConnectArgs[2]?.ToString() ?? "";
            string notice = ConnectArgs[3]?.ToString() ?? "";
            if (msg != "") Console.WriteLine(msg);
            if (serverName != "") Console.WriteLine(serverName);
            if (notice != "") Console.WriteLine(notice);
        }

        public async Task Start()
        {
            ConnectResult result = ConnectResult.CanNotConnect;
            while (result != ConnectResult.Success && Config.FunGame_isAutoRetry)
            {
                try
                {
                    result = await ConnectAsync(TransmittalType.WebSocket, GeneralSettings.FunGameServer, ssl: true, subUrl: "ws");
                    Config.FunGame_isRetrying = false;
                    await Task.Delay(5000);
                }
                catch (Exception e)
                {
                    Error(e);
                }
            }
        }

        public async Task Retry(bool send = false)
        {
            if (HTTPClient is null)
            {
                try
                {
                    if (await ConnectAsync(TransmittalType.WebSocket, GeneralSettings.FunGameServer, ssl: true, subUrl: "ws") == ConnectResult.Success)
                    {
                        Console.WriteLine("重连成功！");
                        if (send)
                        {
                            await Bot.SendFriendMessage(GeneralSettings.Master, "websocket", "重连服务器成功");
                        }
                    }
                    else
                    {
                        Console.WriteLine("重连失败！");
                        if (!Config.FunGame_isRetrying && Config.FunGame_isAutoRetry)
                        {
                            await Task.Delay(5000);
                            if (!Config.FunGame_isRetrying && Config.FunGame_isAutoRetry)
                            {
                                await Retry();
                            }
                        }
                    }
                }
                catch { }
            }
        }
        
        public async Task ConnectToAnonymousServer()
        {
            if (HTTPClient != null)
            {
                await HTTPClient.Send(SocketMessageType.AnonymousGameServer, ServerName);
            }
        }
        
        public async Task DisconnectFromAnonymousServer()
        {
            if (HTTPClient != null)
            {
                await HTTPClient.Send(SocketMessageType.EndGame);
            }
        }
        
        public async Task DisconnectAsync()
        {
            if (HTTPClient != null)
            {
                await HTTPClient.Send(SocketMessageType.Disconnect);
                Close_WebSocket();
            }
        }

        public override async void Error(Exception e)
        {
            Console.WriteLine(e.ToString());
            await DisconnectAsync();
            if (!Config.FunGame_isRetrying && Config.FunGame_isAutoRetry)
            {
                CurrentRetryTimes = -1;
                Config.FunGame_isAutoRetry = true;
                await Task.Delay(5000);
                if (!Config.FunGame_isRetrying && Config.FunGame_isAutoRetry)
                {
                    await Retry();
                }
            }
        }

        public override void WritelnSystemInfo(string msg, LogLevel level = LogLevel.Info, bool useLevel = true)
        {
            Console.WriteLine(msg);
        }

        protected override void SocketHandler_Disconnect(SocketObject ServerMessage)
        {
            Console.WriteLine("断开服务器连接成功");
        }

        protected override async void SocketHandler_AnonymousGameServer(SocketObject ServerMessage)
        {
            Dictionary<string, object> data = ServerMessage.GetParam<Dictionary<string, object>>(0) ?? [];
            if (data.Count > 0)
            {
                long qq = NetworkUtility.JsonDeserializeFromDictionary<long>(data, "qq");
                string msg = NetworkUtility.JsonDeserializeFromDictionary<string>(data, "msg") ?? "";
                if (msg != "")
                {
                    if (qq > 0)
                    {
                        foreach (Group g in Bot.Groups.Where(g => GeneralSettings.FunGameWebSocketGroup.Contains(g.group_id)))
                        {
                            Member m = Bot.GetMember(g.group_id, qq);
                            if (m.user_id == qq)
                            {
                                await Bot.SendGroupMessageAt(qq, g.group_id, "FunGame推送", msg);
                                break;
                            }
                        }
                    }
                    else
                    {
                        long groupid = NetworkUtility.JsonDeserializeFromDictionary<long>(data, "groupid");
                        if (groupid > 0 && Bot.Groups.Any(g => g.group_id == groupid))
                        {
                            await Bot.SendGroupMessage(groupid, "匿名服务器消息", msg);
                        }
                    }
                }
            }
        }

        public async Task SCAdd(long qq, long groupid, double sc = 1)
        {
            if (HTTPClient != null)
            {
                Dictionary<string, object> data = [];
                data.Add("command", "scadd");
                data.Add("qq", qq);
                data.Add("groupid", groupid);
                data.Add("sc", sc);
                await HTTPClient.Send(SocketMessageType.AnonymousGameServer, ServerName, data);
            }
        }

        public async Task SCList(long groupid)
        {
            if (HTTPClient != null)
            {
                Dictionary<string, object> data = [];
                data.Add("command", "sclist");
                data.Add("groupid", groupid);
                await HTTPClient.Send(SocketMessageType.AnonymousGameServer, ServerName, data);
            }
        }
    }
}

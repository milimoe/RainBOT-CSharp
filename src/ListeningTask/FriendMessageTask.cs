using Milimoe.OneBot.Framework;
using Milimoe.OneBot.Model.Content;
using Milimoe.OneBot.Model.Event;
using Milimoe.OneBot.Model.Other;
using Milimoe.OneBot.Model.QuickReply;
using Milimoe.RainBOT.Command;
using Milimoe.RainBOT.Settings;

namespace Milimoe.RainBOT.ListeningTask
{
    public class FriendMessageTask
    {
        private static long dice = 0;

        public static async Task<FriendMsgEventQuickReply?> ListeningTask_handler(FriendMessageEvent e)
        {
            FriendMsgEventQuickReply? quick_reply = null;

            try
            {
                Sender sender = e.sender;
                if (e.user_id == 0 || e.sender.user_id == 0) return quick_reply;

                Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} P/{e.user_id}{(e.detail.Trim() == "" ? "" : " -> " + e.detail)}");
                if (GeneralSettings.IsDebug)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"DEBUG：{e.original_msg}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                if (e.detail == "是")
                {
                    if (e.user_id != GeneralSettings.Master && e.CheckThrow(10, out dice))
                    {
                        Bot.ColorfulCheckPass(sender, "反驳是", dice, 40);
                        await Bot.SendFriendMessage(e.user_id, "随机反驳是", "是你的头");
                    }
                    else if (e.user_id == GeneralSettings.Master)
                    {
                        await Bot.SendFriendMessage(e.user_id, "随机反驳是", "是你的头");
                    }
                }

                // OSM指令
                if (e.detail.Length >= 4 && e.detail[..4] == ".osm")
                {
                    MasterCommand.Execute(e.detail, e.user_id, false, e.user_id, false);
                    return quick_reply;
                }

                if (e.detail.Length >= 9 && e.detail[..9].Equals("FunGame模拟", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.user_id)) return quick_reply;
                    if (!Bot.FunGameSimulation)
                    {
                        Bot.FunGameSimulation = true;
                        List<string> msgs = await Bot.HttpGet<List<string>>("https://api.milimoe.com/fungame/test?isweb=false") ?? [];
                        foreach (string msg in msgs)
                        {
                            await Bot.SendFriendMessage(e.user_id, "FunGame模拟", msg.Trim());
                            await Task.Delay(5500);
                        }
                        Bot.FunGameSimulation = false;
                    }
                    else
                    {
                        await Bot.SendFriendMessage(e.user_id, "FunGame模拟", "游戏正在模拟中，请勿重复请求！");
                    }
                    return quick_reply;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            return quick_reply;
        }
    }
}
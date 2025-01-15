using System.Text.Json;
using Milimoe.OneBot.Model.Event;
using Milimoe.OneBot.Model.Message;
using Milimoe.RainBOT.Settings;

namespace Milimoe.RainBOT.QQBot
{
    public class AppConfig
    {
        public int AppID { get; set; } = 0;
        public string BotToken { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string FunGameServer { get; set; } = "";
    }

    public class QQBotMain
    {
        public static void RunQQBot()
        {
            try
            {
                string json = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"));
                AppConfig config = JsonSerializer.Deserialize<AppConfig>(json) ?? new();
                if (config != null)
                {
                    // 官方BOT服务

                    while (true)
                    {
                        string read = Console.ReadLine() ?? "";
                        if (read == "quit")
                        {
                            break;
                        }
                        if (read == "test")
                        {
                            GroupMessageEvent groupMessageEvent = new();
                            groupMessageEvent.message.Add(new TextMessage("生成20个攻击之爪 +50给123456"));
                            _ = RainBOTFunGame.Handler(groupMessageEvent);
                            continue;
                        }
                        // OSM指令
                        if (read.Length >= 4 && read[..4] == ".osm")
                        {
                            //MasterCommand.Execute(read, GeneralSettings.Master, false, GeneralSettings.Master, false);
                            continue;
                        }
                        switch (read.ToLower().Trim() ?? "")
                        {
                            case "debug on":
                                GeneralSettings.IsDebug = true;
                                Console.WriteLine("开启Debug模式");
                                break;
                            case "debug off":
                                GeneralSettings.IsDebug = false;
                                Console.WriteLine("关闭Debug模式");
                                break;
                        }
                    }
                }
                else throw new Exception("config.json 文件不存在");
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}

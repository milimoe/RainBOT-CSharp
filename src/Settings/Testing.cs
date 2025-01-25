using Milimoe.OneBot.Framework.Utility;
using Milimoe.OneBot.Model.Event;

namespace Milimoe.RainBOT.Settings
{
    public class Testing
    {
        public Testing()
        {
            GeneralSettings.LoadSetting();
            QQOpenID.LoadConfig();
            OshimaController.Config.FunGame_isAutoRetry = true;
            Task r = Task.Run(async () =>
            {
                await OshimaController.Instance.Start();
                await OshimaController.Instance.ConnectToAnonymousServer();
                OshimaController.Config.FunGame_isAutoRetry = true;

                string json = @"{""self_id"":928884953,""user_id"":3305106902,""time"":1737787658,""message_id"":212281255,""real_id"":212281255,""message_seq"":212281255,""message_type"":""group"",""sender"":{""user_id"":3305106902,""nickname"":""心音"",""card"":""高僧预测："",""role"":""admin"",""title"":""注意素质""},""raw_message"":""签到"",""font"":14,""sub_type"":""normal"",""message"":[{""type"":""text"",""data"":{""text"":""签到""}}],""message_format"":""array"",""post_type"":""message"",""group_id"":667678970}";
                try
                {
                    GroupMessageEvent e = JsonTools.GetObject<GroupMessageEvent>(json) ?? new();

                    await RainBOTFunGame.Handler2(e);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
            while (true)
            {
                if (Console.ReadLine() == "test")
                {
                    string json = @"{""self_id"":928884953,""user_id"":3305106902,""time"":1737787658,""message_id"":212281255,""real_id"":212281255,""message_seq"":212281255,""message_type"":""group"",""sender"":{""user_id"":3305106902,""nickname"":""心音"",""card"":""高僧预测："",""role"":""admin"",""title"":""注意素质""},""raw_message"":""签到"",""font"":14,""sub_type"":""normal"",""message"":[{""type"":""text"",""data"":{""text"":""签到""}}],""message_format"":""array"",""post_type"":""message"",""group_id"":667678970}";
                    try
                    {
                        GroupMessageEvent e = JsonTools.GetObject<GroupMessageEvent>(json) ?? new();

                        _ = RainBOTFunGame.Handler2(e);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                if (Console.ReadLine() == "quit")
                    break;
            }
        }
    }
}

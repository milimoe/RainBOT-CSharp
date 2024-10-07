using System.Text.RegularExpressions;
using Milimoe.OneBot.Model.Event;
using Milimoe.OneBot.Model.Message;
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
                
                if (e.user_id == 修仙.小北QQ)
                {
                    Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} P/{e.user_id}{(e.detail.Trim() == "" ? "" : " -> " + Regex.Replace(e.detail, @"\r(?!\n)", "\r\n"))}");
                    if (GeneralSettings.IsDebug)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"DEBUG：{e.original_msg}");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        await Task.Delay(100);
                    }

                    if (修仙状态.炼金药材 && e.message.Where(m => m is MarkdownMessage).FirstOrDefault() is MarkdownMessage md)
                    {
                        await 修仙控制器.自动炼金药材(md.data.data, false, e.user_id);
                    }

                    if (修仙状态.世界BOSS != "" && e.message.Where(m => m is MarkdownMessage).FirstOrDefault() is MarkdownMessage md1)
                    {
                        修仙控制器.打BOSS(md1.data.data, false, e.user_id);
                    }

                    if (修仙状态.悬赏令 && e.message.Where(m => m is MarkdownMessage).FirstOrDefault() is MarkdownMessage md2)
                    {
                        修仙控制器.自动悬赏令(md2.data.data, false, e.user_id);
                    }

                    if (修仙状态.秘境 && e.message.Where(m => m is MarkdownMessage).FirstOrDefault() is MarkdownMessage md3)
                    {
                        修仙控制器.自动秘境(md3.data.data, false, e.user_id);
                    }

                    return quick_reply;
                }

                if (e.user_id == GeneralSettings.Master)
                {
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
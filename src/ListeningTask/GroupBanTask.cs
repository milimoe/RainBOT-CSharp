using System.Reflection;
using Milimoe.OneBot.Framework;
using Milimoe.OneBot.Model.Content;
using Milimoe.OneBot.Model.Event;
using Milimoe.OneBot.Model.Other;
using Milimoe.RainBOT.Settings;

namespace Milimoe.RainBOT.ListeningTask
{
    public class GroupBanTask
    {
        public static async void ListeningTask_handler(GroupBanEvent e)
        {
            try
            {
                Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} N/Group_Ban G/{e.group_id}{(e.detail.Trim() == "" ? "" : " -> " + e.detail)}");
                if (GeneralSettings.IsDebug)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"DEBUG：{e.original_msg}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                if (e.sub_type == "ban" && e.duration > 0 && e.user_id == GeneralSettings.Master && e.operator_id != GeneralSettings.BotQQ)
                {
                    MuteRecall.Muted[e.group_id].Remove(e.user_id);
                    MuteRecall.Muted[e.group_id].Add(e.user_id, GeneralSettings.Master);
                    SetGroupBanContent content_unmute_master = new(e.group_id, GeneralSettings.Master, 0);
                    SetGroupBanContent content_mute_operator = new(e.group_id, e.operator_id, 60);
                    await GroupMessageTask.Post(SupportedAPI.set_group_ban, e.group_id, "反制禁言", [content_unmute_master, content_mute_operator]);
                    Member sender = Bot.GetMember(e.group_id, e.operator_id);
                    await e.SendMessage($"检测到主人被{sender.user_id}（{(sender.card != "" ? sender.card : sender.nickname)}）禁言！");
                }
                else if (e.sub_type == "ban" && e.duration > 0)
                {
                    MuteRecall.Muted[e.group_id].Remove(e.user_id);
                    MuteRecall.Muted[e.group_id].Add(e.user_id, e.operator_id);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}
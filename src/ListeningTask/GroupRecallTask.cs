using Milimoe.OneBot.Model.Event;
using Milimoe.RainBOT.Settings;

namespace Milimoe.RainBOT.ListeningTask
{
    public class GroupRecallTask
    {
        public static Dictionary<string, DateTime> Recalls { get; } = [];

        public static async Task ListeningTask_handler(GroupRecallEvent e)
        {
            try
            {
                Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} N/Group_Recall G/{e.group_id}{(e.detail.Trim() == "" ? "" : " -> " + e.detail)}");
                if (GeneralSettings.IsDebug)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"DEBUG：{e.original_msg}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                Recalls[e.detail] = DateTime.Now;
                await Task.CompletedTask;
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
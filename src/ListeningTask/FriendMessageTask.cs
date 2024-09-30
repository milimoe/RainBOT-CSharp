using System.Text.RegularExpressions;
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
        public static bool 正在悬赏令 { get; set; } = false;
        public static bool 秘境 { get; set; } = false;
        public static bool 闭关 { get; set; } = false;
        public static bool 修炼 { get; set; } = true;
        public static bool 炼金药材 { get; set; } = false;
        public static string 世界BOSS { get; set; } = "";
        public static int 修炼次数 { get; set; } = 6;

        private static long dice = 0;

        public static async Task<FriendMsgEventQuickReply?> ListeningTask_handler(FriendMessageEvent e)
        {
            FriendMsgEventQuickReply? quick_reply = null;

            try
            {
                Sender sender = e.sender;
                
                if (e.user_id == 3889029313)
                {
                    Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} P/{e.user_id}{(e.detail.Trim() == "" ? "" : " -> " + Regex.Replace(e.detail, @"\r(?!\n)", "\r\n"))}");
                    if (GeneralSettings.IsDebug)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"DEBUG：{e.original_msg}");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        await Task.Delay(100);
                    }

                    if (炼金药材)
                    {
                        炼金药材 = false;

                        // 正则表达式提取名字
                        MatchCollection names = Regex.Matches(e.detail, @"名字：(.+?)(?=\r)", RegexOptions.Singleline);
                        MatchCollection quantitys = Regex.Matches(e.detail, @"拥有数量：(\d+)");

                        for (int i = 0; i < names.Count; i++)
                        {
                            string name = names[i].Groups[1].Value.Trim();
                            int quantity = int.Parse(quantitys[i].Groups[1].Value);
                            await Bot.SendFriendMessage(e.user_id, "炼金药材", "炼金 " + name + " " + quantity);
                            await Task.Delay(2000);
                        }

                        if (e.detail.Contains('页'))
                        {
                            炼金药材 = true;
                            await Task.Delay(5000);
                            await Bot.SendFriendMessage(e.user_id, "炼金药材", "药材背包");
                        }
                    }

                    if (世界BOSS != "")
                    {
                        // 使用正则表达式匹配编号和BOSS名字
                        string pattern = $@"编号(\d+)、{世界BOSS}Boss:([\u4e00-\u9fa5A-Za-z]+)\s*\r";
                        MatchCollection matches = Regex.Matches(e.raw_message, pattern);
                        世界BOSS = "";

                        // 创建字典存储匹配到的编号和名字
                        Dictionary<int, string> bossDictionary = [];

                        foreach (Match match in matches)
                        {
                            int number = int.Parse(match.Groups[1].Value);
                            string bossName = match.Groups[2].Value.Trim();
                            bossDictionary[number] = bossName;
                        }

                        if (bossDictionary.Count > 0)
                        {
                            int id = bossDictionary.Keys.Last();
                            await Bot.SendFriendMessage(e.user_id, "BOSS", "讨伐世界boss " + id);
                        }
                        else Console.WriteLine("没有BOSS了");
                    }

                    if (正在悬赏令)
                    {
                        正在悬赏令 = false;
                        if (e.detail.Contains("灵石不足以刷新"))
                        {
                            修炼 = true;
                            return quick_reply;
                        }
                        悬赏令? x = 悬赏令.获取最好的悬赏令(e.detail);
                        int time = x?.Duration ?? 40;
                        await Bot.SendFriendMessage(e.user_id, "悬赏令", "悬赏令接取" + (x?.Id ?? 0));
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay((time + 4) * 60 * 1000);
                            await Bot.SendFriendMessage(e.user_id, "悬赏令", "悬赏令结算");
                            await Task.Delay(5 * 1000);
                            正在悬赏令 = true;
                            await Bot.SendFriendMessage(e.user_id, "悬赏令", "悬赏令刷新");
                        });
                    }

                    if (秘境)
                    {
                        秘境 = false;
                        if (e.detail.Contains("已耗尽"))
                        {
                            修炼 = true;
                            return quick_reply;
                        }
                        // 正则表达式用于提取时间
                        string pattern = @"(\d+)\s*分钟";
                        Match match = Regex.Match(e.detail, pattern);
                        if (match.Success)
                        {
                            string time = match.Groups[1].Value;
                            if (!int.TryParse(time, out int realTime))
                            {
                                realTime = 200;
                            }
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay((realTime + 4) * 60 * 1000);
                                await Bot.SendFriendMessage(e.user_id, "秘境", "秘境结算");
                                修炼 = true;
                            });
                        }
                    }
                }

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

                if (e.detail == "挑战结束" && e.user_id == GeneralSettings.Master)
                {
                    await Bot.Unmute12ClockMembers();
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

                if (GeneralSettings.IsMute && e.detail == "忏悔")
                {
                    if (!await Bot.CheckBlackList(false, e.user_id, e.user_id)) return quick_reply;
                    string msg = "";
                    foreach (long group_id in Bot.Groups.Select(g => g.group_id))
                    {
                        if (Bot.BotIsAdmin(group_id) && MuteRecall.Muted[group_id].TryGetValue(e.user_id, out long operator_id) && operator_id == Bot.BotQQ)
                        {
                            MuteRecall.Muted[group_id].Remove(e.user_id);
                            await Bot.SendMessage(SupportedAPI.set_group_ban, group_id, "忏悔", new SetGroupBanContent(group_id, e.user_id, 0), true);
                            if (msg != "") msg += "\r\n";
                            msg += $"[{group_id}] 忏悔成功！！希望你保持纯真，保持野性的美。";
                        }
                    }
                    if (msg == "") msg = "你无需忏悔。请注意：我不能帮你解除由管理员手动操作的禁言。";
                    await Bot.SendFriendMessage(e.user_id, "忏悔", msg);
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
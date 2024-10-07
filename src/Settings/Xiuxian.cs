using System.Text.RegularExpressions;
using Milimoe.OneBot.Framework.Utility;
using Milimoe.OneBot.Model.Content;
using Milimoe.OneBot.Model.Message;

namespace Milimoe.RainBOT.Settings
{
    public class 修仙
    {
        public static bool 私聊模式 { get; set; } = true;
        public static long 指定群聊 { get; set; } = 0;
        public static long 小北QQ { get; set; } = 3889029313;
        public static long 小小QQ { get; set; } = 3889001741;
        public static bool 开启自动修炼 { get; set; } = false;
        public static bool 开启小小修炼 { get; set; } = false;
        public static bool 开启自动悬赏令 { get; set; } = false;
        public static bool 开启自动秘境 { get; set; } = false;
        public static bool 开启自动灵田收取宗门丹药领取 { get; set; } = false;
        public static bool 开启自动突破 { get; set; } = false;
        public static long 每修炼几次破一次 { get; set; } = 5;
        public static PluginConfig Configs { get; set; } = new("rainbot", "xiuxian");

        public static void Init()
        {
            Configs.Load();
            if (Configs.TryGetValue("私聊模式", out object? value) && value is bool bol)
            {
                私聊模式 = bol;
            }
            else Configs.Add("私聊模式", 私聊模式);
            if (Configs.TryGetValue("指定群聊", out object? value2) && value2 is long group)
            {
                指定群聊 = group;
            }
            else Configs.Add("指定群聊", 指定群聊);
            if (Configs.TryGetValue("开启自动修炼", out value) && value is bool bol1)
            {
                开启自动修炼 = bol1;
            }
            else Configs.Add("开启自动修炼", 开启自动修炼);
            if (Configs.TryGetValue("开启小小修炼", out value) && value is bool bol2)
            {
                开启小小修炼 = bol2;
            }
            else Configs.Add("开启小小修炼", 开启小小修炼);
            if (Configs.TryGetValue("开启自动悬赏令", out value) && value is bool bol3)
            {
                开启自动悬赏令 = bol3;
            }
            else Configs.Add("开启自动悬赏令", 开启自动悬赏令);
            if (Configs.TryGetValue("开启自动秘境", out value) && value is bool bol4)
            {
                开启自动秘境 = bol4;
            }
            else Configs.Add("开启自动秘境", 开启自动秘境);
            if (Configs.TryGetValue("开启自动灵田收取宗门丹药领取", out value) && value is bool bol5)
            {
                开启自动灵田收取宗门丹药领取 = bol5;
            }
            else Configs.Add("开启自动灵田收取宗门丹药领取", 开启自动灵田收取宗门丹药领取);
            if (Configs.TryGetValue("开启自动突破", out value) && value is bool bol6)
            {
                开启自动突破 = bol6;
            }
            else Configs.Add("开启自动突破", 开启自动突破);
            if (Configs.TryGetValue("每修炼几次破一次", out value2) && value2 is long l)
            {
                每修炼几次破一次 = l;
            }
            else Configs.Add("每修炼几次破一次", 每修炼几次破一次);
            Configs.Save();
        }

        public static async Task 发消息(string function, string text)
        {
            if (私聊模式)
            {
                await Bot.SendFriendMessage(小北QQ, function, text);
                // if (开启小小修炼) await Bot.SendFriendMessage(小小QQ, function, text);
            }
            else
            {
                GroupMessageContent content = new(指定群聊);
                content.message.Add(new AtMessage(小北QQ));
                content.message.Add(new TextMessage(text));
                await Bot.SendGroupMessage(指定群聊, function, content);
                if (开启小小修炼)
                {
                    GroupMessageContent content2 = new(指定群聊);
                    content2.message.Add(new AtMessage(小小QQ));
                    content2.message.Add(new TextMessage(text));
                    await Bot.SendGroupMessage(指定群聊, function, content2);
                }
            }
        }
    }

    public class 修仙状态
    {
        public static bool 闭关 { get; set; } = false;
        public static bool 悬赏令 { get; set; } = false;
        public static bool 秘境 { get; set; } = false;
        public static bool 炼金药材 { get; set; } = false;
        public static string 世界BOSS { get; set; } = "";
        public static long 修炼次数 { get; set; } = 修仙.每修炼几次破一次 - 1 < 0 ? 0 : 修仙.每修炼几次破一次 - 1;
    }

    public class 修仙控制器
    {
        public static async Task 自动炼金药材(string detail, bool is_group, long target_id)
        {
            if (修仙状态.炼金药材)
            {
                修仙状态.炼金药材 = false;

                // 正则表达式提取名字
                MatchCollection names = Regex.Matches(detail, @"名字：(.+?)(?=\r)", RegexOptions.Singleline);
                MatchCollection quantitys = Regex.Matches(detail, @"拥有数量：(\d+)");
                Dictionary<string, int> dict = [];

                for (int i = 0; i < names.Count; i++)
                {
                    string name = names[i].Groups[1].Value.Trim();
                    int quantity = int.Parse(quantitys[i].Groups[1].Value);
                    if (!dict.TryAdd(name, quantity))
                    {
                        dict[name] += quantity;
                    }
                }
                
                foreach (string name in dict.Keys)
                {
                    Task t = is_group ? Bot.SendGroupMessage(target_id, "炼金药材", "炼金 " + name + " " + dict[name]) : Bot.SendFriendMessage(target_id, "炼金药材", "炼金 " + name + " " + dict[name]);
                    await t;
                    await Task.Delay(2000);
                }

                if (detail.Contains('页'))
                {
                    修仙状态.炼金药材 = true;
                    await Task.Delay(5000);
                    _ = is_group ? Bot.SendGroupMessage(target_id, "炼金药材", "药材背包") : Bot.SendFriendMessage(target_id, "炼金药材", "药材背包");
                }
            }
        }

        public static void 打BOSS(string detail, bool is_group, long target_id)
        {
            if (修仙状态.世界BOSS != "")
            {
                // 使用正则表达式匹配编号和BOSS名字
                string pattern = $@"编号(\d+)、{修仙状态.世界BOSS}Boss:([\u4e00-\u9fa5A-Za-z]+)\s*\r";
                MatchCollection matches = Regex.Matches(detail, pattern);
                修仙状态.世界BOSS = "";

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
                    _ = is_group ? Bot.SendGroupMessage(target_id, "BOSS", "讨伐世界boss " + id) : Bot.SendFriendMessage(target_id, "BOSS", "讨伐世界boss " + id);
                }
                else Console.WriteLine("没有BOSS了");
            }
        }

        public static async void 自动悬赏令(string detail, bool is_group, long target_id)
        {
            if (修仙状态.悬赏令)
            {
                修仙状态.悬赏令 = false;
                悬赏令? x = 悬赏令.获取最好的悬赏令(detail);
                if (detail.Contains("灵石不足以刷新") || detail.Contains("已耗尽") || detail.Contains("已用尽") || x is null)
                {
                    Console.WriteLine("做完了悬赏令");
                    修仙.开启自动修炼 = true;
                    修仙.开启自动悬赏令 = true;
                    return;
                }
                int time = x.Duration;
                Task t = is_group ? Bot.SendGroupMessage(target_id, "悬赏令", "悬赏令接取" + (x.Id)) : Bot.SendFriendMessage(target_id, "悬赏令", "悬赏令接取" + (x.Id));
                await t;
                _ = Task.Run(async () =>
                {
                    await Task.Delay((time + 4) * 60 * 1000);
                    Task t1 = is_group ? Bot.SendFriendMessage(target_id, "悬赏令", "悬赏令结算") : Bot.SendFriendMessage(target_id, "悬赏令", "悬赏令结算");
                    await t1;
                    await Task.Delay(5 * 1000);
                    修仙状态.悬赏令 = true;
                    Task t2 = is_group ? Bot.SendGroupMessage(target_id, "悬赏令", "悬赏令刷新") : Bot.SendFriendMessage(target_id, "悬赏令", "悬赏令刷新");
                    await t2;
                });
            }
        }

        public static void 自动秘境(string detail, bool is_group, long target_id)
        {
            if (修仙状态.秘境)
            {
                修仙状态.秘境 = false;
                if (detail.Contains("参加过本次"))
                {
                    修仙.开启自动修炼 = true;
                    修仙.开启自动秘境 = true;
                    return;
                }
                // 正则表达式用于提取时间
                string pattern = @"(\d+)\s*分钟";
                Match match = Regex.Match(detail, pattern);
                if (match.Success)
                {
                    string time = match.Groups[1].Value;
                    if (!int.TryParse(time, out int realTime))
                    {
                        修仙.开启自动修炼 = true;
                        修仙.开启自动秘境 = true;
                        return;
                    }
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay((realTime + 4) * 60 * 1000);
                        Task t = is_group ? Bot.SendGroupMessage(target_id, "秘境", "秘境结算") : Bot.SendFriendMessage(target_id, "秘境", "秘境结算");
                        await t;
                        修仙.开启自动修炼 = true;
                        修仙.开启自动秘境 = true;
                    });
                }
            }
        }
    }

    public class 悬赏令(int id, string name, int completionRate, int baseReward, int duration, string possibleReward)
    {
        public int Id { get; set; } = id;
        public string Name { get; set; } = name;
        public int CompletionRate { get; set; } = completionRate;
        public int BaseReward { get; set; } = baseReward;
        public int Duration { get; set; } = duration;
        public string PossibleReward { get; set; } = possibleReward;
        public string RewardRank { get; set; } = GetRank(possibleReward);

        // 提取品阶等级，并赋予优先级数值
        private static string GetRank(string reward)
        {
            if (reward.Contains("仙阶")) return "仙阶";
            if (reward.Contains("天阶")) return "天阶";
            if (reward.Contains("九品")) return "九品";
            if (reward.Contains("八品")) return "八品";
            if (reward.Contains("地阶")) return "地阶";
            if (reward.Contains("七品")) return "七品";
            if (reward.Contains("六品")) return "六品";
            if (reward.Contains("玄阶")) return "玄阶";
            if (reward.Contains("五品")) return "五品";
            if (reward.Contains("四品")) return "四品";
            if (reward.Contains("黄阶")) return "黄阶";
            return "无";
        }


        public static 悬赏令? 获取最好的悬赏令(string description)
        {
            string pattern = @"(\d+)、(.+?),完成机率(\d+),基础报酬(\d+)修为,预计需(\d+)分钟，可能额外获得：(.*)!";
            MatchCollection matches = Regex.Matches(description, pattern);

            List<悬赏令> tasks = [];

            foreach (Match match in matches)
            {
                int id = int.Parse(match.Groups[1].Value);
                string name = match.Groups[2].Value.Trim();
                int completionRate = int.Parse(match.Groups[3].Value);
                int baseReward = int.Parse(match.Groups[4].Value);
                int duration = int.Parse(match.Groups[5].Value);
                string possibleReward = match.Groups[6].Value.Trim();

                tasks.Add(new 悬赏令(id, name, completionRate, baseReward, duration, possibleReward));
            }

            // 按优先级排序：完成几率优先，品阶最高次之，基础报酬最后
            悬赏令? bestTask = tasks.OrderByDescending(t => t.CompletionRate)
                                .ThenBy(t => GetPriority(t.RewardRank))
                                .ThenByDescending(t => t.BaseReward)
                                .FirstOrDefault();

            if (bestTask != null)
            {
                Console.WriteLine($"最符合条件的任务是: {bestTask.Id + "、" + bestTask.Name}");
                return bestTask;
            }
            return null;
        }

        // 根据奖励等级返回优先级数值，数值越低优先级越高
        public static int GetPriority(string rank)
        {
            return rank switch
            {
                "仙阶" => 1,
                "天阶" => 2,
                "九品" => 2,
                "八品" => 3,
                "地阶" => 4,
                "七品" => 5,
                "六品" => 6,
                "玄阶" => 7,
                "五品" => 8,
                "四品" => 9,
                "黄阶" => 10,
                _ => 11,
            };
        }
    }
}

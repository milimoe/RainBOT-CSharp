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
            小北 = new(小北QQ);
            小小 = new(小小QQ);
            Configs.Save();
        }

        public static async Task 发消息(string function, string text, long qq = 0)
        {
            if (私聊模式)
            {
                if (qq != 0)
                {
                    await Bot.SendFriendMessage(qq, function, text);
                }
                else
                {
                    await Bot.SendFriendMessage(小北QQ, function, text);
                    // if (开启小小修炼) await Bot.SendFriendMessage(小小QQ, function, text);
                }
            }
            else
            {
                GroupMessageContent content = new(指定群聊);
                if (qq != 0)
                {
                    content.message.Add(new AtMessage(qq));
                }
                else
                {
                    content.message.Add(new AtMessage(小北QQ));
                }
                content.message.Add(new TextMessage(text));
                await Bot.SendGroupMessage(指定群聊, function, content);
                if (开启小小修炼 && qq == 0)
                {
                    GroupMessageContent content2 = new(指定群聊);
                    content2.message.Add(new AtMessage(小小QQ));
                    content2.message.Add(new TextMessage(text));
                    await Bot.SendGroupMessage(指定群聊, function, content2);
                }
            }
        }

        public static 修仙控制器 小北 { get; set; } = new(小北QQ);
        public static 修仙控制器 小小 { get; set; } = new(小小QQ);
    }

    public class 修仙状态
    {
        public bool 闭关 { get; set; } = false;
        public bool 悬赏令 { get; set; } = false;
        public bool 秘境 { get; set; } = false;
        public bool 在做悬赏令 { get; set; } = false;
        public bool 在秘境中 { get; set; } = false;
        public bool 炼金药材 { get; set; } = false;
        public string 世界BOSS { get; set; } = "";
        public long 修炼次数 { get; set; } = 修仙.每修炼几次破一次 - 1 < 0 ? 0 : 修仙.每修炼几次破一次 - 1;
    }

    public class 修仙控制器(long qq)
    {
        public 修仙状态 修仙状态 = new();
        public bool 开启修炼 = false;

        public async Task 自动炼金药材(string detail)
        {
            if (修仙状态.炼金药材)
            {
                修仙状态.炼金药材 = false;
                detail = detail.Replace(": ", "：").Replace(":", "：");

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
                    await 修仙.发消息("炼金药材", "炼金 " + name + " " + dict[name], qq);
                    await Task.Delay(2000);
                }

                if (detail.Contains('页'))
                {
                    修仙状态.炼金药材 = true;
                    await Task.Delay(3000);
                    await 修仙.发消息("炼金药材", "药材背包", qq);
                }
            }
        }

        public void 打BOSS(string detail)
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
                    _ = 修仙.发消息("讨伐世界boss", "讨伐世界boss " + id, qq);
                }
                else Console.WriteLine("没有BOSS了");
            }
        }

        public async void 自动悬赏令(string detail)
        {
            if (修仙状态.悬赏令)
            {
                修仙状态.悬赏令 = false;
                修仙状态.在做悬赏令 = true;
                悬赏令? x = 悬赏令.获取最好的悬赏令(qq, detail);
                if (detail.Contains("灵石不足以刷新") || detail.Contains("已耗尽") || detail.Contains("已用尽") || x is null)
                {
                    Console.WriteLine("做完了悬赏令");
                    修仙.开启自动修炼 = true;
                    修仙.开启自动悬赏令 = true;
                    修仙状态.在做悬赏令 = false;
                    return;
                }
                int time = x.Duration;
                await Task.Delay(1500);
                await 修仙.发消息("悬赏令", "悬赏令接取" + x.Id, qq);
                _ = Task.Run(async () =>
                {
                    await Task.Delay((time + 4) * 60 * 1000);
                    await 修仙.发消息("悬赏令", "悬赏令结算", qq);
                    await Task.Delay(5 * 1000);
                    修仙状态.悬赏令 = true;
                    await 修仙.发消息("悬赏令", "悬赏令刷新", qq);
                });
            }
        }

        public void 自动秘境(string detail)
        {
            if (修仙状态.秘境)
            {
                修仙状态.秘境 = false;
                修仙状态.在秘境中 = true;
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
                        await 修仙.发消息("秘境", "秘境结算", qq);
                        修仙.开启自动修炼 = true;
                        修仙.开启自动秘境 = true;
                        修仙状态.在秘境中 = false;
                    });
                }
            }
        }
    }

    public class 悬赏令(int id, string name, int completionRate, long baseReward, int duration, string possibleReward)
    {
        public int Id { get; set; } = id;
        public string Name { get; set; } = name;
        public int CompletionRate { get; set; } = completionRate;
        public long BaseReward { get; set; } = baseReward;
        public int Duration { get; set; } = duration;
        public string PossibleReward { get; set; } = possibleReward;
        public double AverageScore { get; set; } = 0;
        public string Description { get; set; } = "";

        public static 悬赏令? 获取最好的悬赏令(long qq, string description)
        {
            string pattern = "";
            if (qq == 修仙.小小QQ)
            {
                pattern = @"(\d+)、([^,]+),完成机率(\d+),基础报酬(\d+)修为,预计需(\d+)分钟，可能额外获得：([^!]+)!";
            }
            else
            {
                pattern = @"(\d+)、([^,]+), 完成机率 (\d+)%?， 基础报酬 (\d+) 修为， 预计需 (\d+) 分钟 ，可能额外获得：([^ \n]+)";
            }
            MatchCollection matches = Regex.Matches(description, pattern);

            List<悬赏令> tasks = [];

            foreach (Match match in matches)
            {
                int id = int.Parse(match.Groups[1].Value);
                string name = match.Groups[2].Value.Trim();
                int completionRate = int.Parse(match.Groups[3].Value);
                long baseReward = long.Parse(match.Groups[4].Value);
                int duration = int.Parse(match.Groups[5].Value);
                string possibleReward = match.Groups[6].Value.Trim();

                悬赏令 xsl = new(id, name, completionRate, baseReward, duration, possibleReward);
                xsl.CalculateWeightedScore();
                tasks.Add(xsl);
            }

            悬赏令? bestTask = tasks.OrderByDescending(x => x.AverageScore).FirstOrDefault();

            if (bestTask != null)
            {
                _ = Bot.SendFriendMessage(GeneralSettings.Master, "悬赏令", (qq == 修仙.小北QQ ? "【小北】" : "【小小】") + " 接取了任务：" + bestTask.Description);
                return bestTask;
            }
            return null;
        }
        
        // 定义物品品阶及其权重
        public static Dictionary<string, int> RankWeights { get; } = new()
        {
            { "仙阶极品", 100 },
            { "仙阶上品", 90 },
            { "仙阶下品", 80 },
            { "天阶上品", 70 },
            { "天阶下品", 60 },
            { "地阶上品", 50 },
            { "地阶下品", 40 },
            { "玄阶上品", 30 },
            { "玄阶下品", 25 },
            { "黄阶上品", 20 },
            { "黄阶下品", 15 },
            { "人阶上品", 10 },
            { "人阶下品", 5 },
            { "九品药材", 85 },
            { "八品药材", 65 },
            { "七品药材", 50 },
            { "六品药材", 40 },
            { "五品药材", 30 },
            { "四品药材", 20 },
            { "三品药材", 15 },
            { "二品药材", 10 },
            { "一品药材", 5 }
        };

        // 计算加权得分并生成任务描述
        public void CalculateWeightedScore()
        {
            // 设置权重系数
            double completionWeight;
            // 根据完成几率的区间设定权重
            if (CompletionRate > 0.7)
            {
                completionWeight = 100; // 完成几率大于 70%
            }
            else if (CompletionRate > 0.5)
            {
                completionWeight = 40; // 完成几率介于 50% 和 70% 之间
            }
            else if (CompletionRate > 0.2)
            {
                completionWeight = 20; // 完成几率介于 20% 和 50% 之间
            }
            else
            {
                completionWeight = 0; // 完成几率小于 20%
            }

            // 使用品阶权重字典计算额外奖励的权重，并引入优先系数
            int extraRewardWeight = RankWeights.Where(kv => PossibleReward.Contains(kv.Key)).Select(kv => kv.Value).FirstOrDefault();

            // 引入优先系数：对于权重大于50的奖励，额外奖励权重增加 1.5 倍
            double extraRewardPriorityWeight = extraRewardWeight > 50 ? extraRewardWeight * 1.5 : extraRewardWeight;

            // 基础报酬权重：避免影响优先级判断
            double rewardWeight = BaseReward * 0.0000000000005;

            // 计算加权平均分，只考虑完成几率和额外奖励权重
            AverageScore = (completionWeight + extraRewardPriorityWeight + rewardWeight) / 3;
            //AverageScore = (completionWeight + extraRewardPriorityWeight) / 2;

            // 生成任务消息
            Description = $"{Id}、完成几率: {CompletionRate}%, 基础报酬: {BaseReward} 修为, " +
                      $"预计时间: {Duration} 分钟, 可能额外获得: {PossibleReward} (权重: {extraRewardWeight})";
        }
    }
}

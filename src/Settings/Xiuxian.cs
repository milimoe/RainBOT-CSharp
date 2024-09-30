using System.Text.RegularExpressions;

namespace Milimoe.RainBOT.Settings
{
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

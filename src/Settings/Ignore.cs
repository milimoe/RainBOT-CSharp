namespace Milimoe.RainBOT.Settings
{
    public class Ignore
    {
        public static HashSet<string> RepeatIgnore { get; } = [
            "我的运势",
            "来图",
            "白毛",
            "猫耳",
            "壁纸",
            "新闻",
            "菜单",
            "白毛",
            "http:",
            "https:",
            ".com",
            ".cn",
            ".osm",
            "[at=all]",
            "[聊天记录]",
            "禁言抽奖",
            "撤回；",
            "/撤回",
            "白丝",
            "黑丝"
        ];

        public static List<long> IgnoreQQGroup { get; } = [

        ];
    }
}

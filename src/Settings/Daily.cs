using Milimoe.OneBot.Framework.Utility;

namespace Milimoe.RainBOT.Settings
{
    public class Daily
    {
        public static bool DailyNews { get; set; } = false;

        public static bool ClearDailys { get; set; } = true;

        public static Dictionary<long, string> UserDailys { get; } = [];

        public static List<string> DailyContent { get; set; } = [];

        public static void InitDaily()
        {
            PluginConfig configs = new("rainbot", "daily");
            configs.Load();
            if (configs.TryGetValue("DailyContent", out object? value) && value != null)
            {
                DailyContent = (List<string>)value;
            }
        }
    }
}

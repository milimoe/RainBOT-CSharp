using Milimoe.OneBot.Framework.Utility;

namespace Milimoe.RainBOT.Settings
{
    public class Ignore
    {
        public static HashSet<string> RepeatIgnore { get; set; } = [];

        public static List<long> IgnoreQQGroup { get; set; } = [];

        public static void InitIgnore()
        {
            PluginConfig configs = new("rainbot", "ignore");
            configs.Load();
            if (configs.TryGetValue("RepeatIgnore", out object? value) && value != null)
            {
                RepeatIgnore = new HashSet<string>((List<string>)value);
            }
            if (configs.TryGetValue("IgnoreQQGroup", out value) && value != null)
            {
                IgnoreQQGroup = (List<long>)value;
            }
        }
    }
}

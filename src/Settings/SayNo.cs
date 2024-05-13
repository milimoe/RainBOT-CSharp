using Milimoe.OneBot.Framework.Utility;

namespace Milimoe.RainBOT.Settings
{
    public class SayNo
    {
        public static HashSet<string> Trigger { get; set; } = [];

        public static HashSet<string> TriggerBeforeNo { get; set; } = [];

        public static HashSet<string> IgnoreTriggerAfterNo { get; set; } = [];

        public static HashSet<string> IgnoreTriggerBeforeCan { get; set; } = [];

        public static HashSet<string> TriggerAfterYes { get; set; } = [];

        public static HashSet<string> WillNotSayNo { get; set; } = [];

        public static List<string> SayNoWords { get; set; } = [];

        public static List<string> SayDontHaveWords { get; set; } = [];

        public static List<string> SayNotYesWords { get; set; } = [];

        public static List<string> SayDontWords { get; set; } = [];

        public static List<string> SayWantWords { get; set; } = [];

        public static List<string> SayThinkWords { get; set; } = [];

        public static List<string> SaySpecialNoWords { get; set; } = [];

        public static void InitSayNo()
        {
            PluginConfig configs = new("rainbot", "sayno");
            configs.Load();
            foreach (string key in configs.Keys)
            {
                if (configs.TryGetValue(key, out object? value) && value != null)
                {
                    switch (key)
                    {
                        case "Trigger":
                            Trigger = new HashSet<string>((List<string>)value);
                            break;
                        case "TriggerBeforeNo":
                            TriggerBeforeNo = new HashSet<string>((List<string>)value);
                            break;
                        case "IgnoreTriggerAfterNo":
                            IgnoreTriggerAfterNo = new HashSet<string>((List<string>)value);
                            break;
                        case "IgnoreTriggerBeforeCan":
                            IgnoreTriggerBeforeCan = new HashSet<string>((List<string>)value);
                            break;
                        case "TriggerAfterYes":
                            TriggerAfterYes = new HashSet<string>((List<string>)value);
                            break;
                        case "WillNotSayNo":
                            WillNotSayNo = new HashSet<string>((List<string>)value);
                            break;
                        case "SayNoWords":
                            SayNoWords = (List<string>)value;
                            break;
                        case "SayDontHaveWords":
                            SayDontHaveWords = (List<string>)value;
                            break;
                        case "SayNotYesWords":
                            SayNotYesWords = (List<string>)value;
                            break;
                        case "SayDontWords":
                            SayDontWords = (List<string>)value;
                            break;
                        case "SayWantWords":
                            SayWantWords = (List<string>)value;
                            break;
                        case "SayThinkWords":
                            SayThinkWords = (List<string>)value;
                            break;
                        case "SaySpecialNoWords":
                            SaySpecialNoWords = (List<string>)value;
                            break;
                    }
                }
            }
        }
}

    }
namespace Milimoe.RainBOT.Settings
{
    public class MuteRecall
    {
        public static Dictionary<long, long> WillMute { get; } = [];
        public static Dictionary<long, Dictionary<long, long>> Muted { get; } = [];
    }
}

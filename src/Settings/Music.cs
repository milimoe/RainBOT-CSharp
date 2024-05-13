using Milimoe.OneBot.Framework.Utility;

namespace Milimoe.RainBOT.Settings
{
    public class Music
    {
        public static Dictionary<string, string> MusicList { get; set; } = [];

        public static void InitMusicList()
        {
            PluginConfig configs = new("rainbot", "musiclist");
            configs.Load();
            foreach (string key in configs.Keys)
            {
                if (configs.TryGetValue(key, out object? value) && value != null && value.GetType() == typeof(string))
                {
                    if (MusicList.ContainsKey(key)) MusicList[key] = (string)value;
                    else MusicList.Add(key, (string)value);
                }
            }
        }
    }
}

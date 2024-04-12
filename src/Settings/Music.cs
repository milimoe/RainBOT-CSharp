using Milimoe.OneBot.Framework.Utility;

namespace Milimoe.RainBOT.Settings
{
    public class Music
    {
        public static Dictionary<string, string> MusicList { get; set; } = [];

        public static void InitMusicList()
        {
            PluginConfig Configs = new("rainbot", "musiclist");
            Configs.Load();
            foreach (string key in Configs.Keys)
            {
                if (Configs.TryGetValue(key, out object? value) && value != null && value.GetType() == typeof(string))
                {
                    if (MusicList.ContainsKey(key)) MusicList[key] = (string)value;
                    else MusicList.Add(key, (string)value);
                }
            }
        }
    }
}

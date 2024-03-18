namespace Milimoe.RainBOT.Settings
{
    public class Music
    {
        /// <summary>
        /// 目前支持的语音包：<para/>
        /// ikun<para/>
        /// 懂CSGO<para/>
        /// 令人沮丧的游戏<para/>
        /// man<para/>
        /// </summary>
        public static Dictionary<string, string> MusicList { get; set; } = [];

        public static void InitMusicList()
        {
            MusicList.Add("ikun", "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"music\ikun.mp3");
            MusicList.Add("懂CSGO", "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"music\懂CSGO.mp3");
            MusicList.Add("令人沮丧的游戏", "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"music\令人沮丧的游戏.mp3");
            MusicList.Add("man", "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"music\man.mp3");
        }
    }
}

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
        /// 马云<para/>
        /// 疤王<para/>
        /// 电锯/追命<para/>
        /// 终极<para/>
        /// </summary>
        public static Dictionary<string, string> MusicList { get; set; } = [];

        public static void InitMusicList()
        {
            MusicList.Add("ikun", "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"music\ikun.mp3");
            MusicList.Add("懂CSGO", "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"music\懂CSGO.mp3");
            MusicList.Add("令人沮丧的游戏", "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"music\令人沮丧的游戏.mp3");
            MusicList.Add("man", "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"music\man.mp3");
            MusicList.Add("马云", "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"music\马云.mp3");
            MusicList.Add("疤王", "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"music\疤王.mp3");
            MusicList.Add("电锯", "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"music\电锯.mp3");
            MusicList.Add("终极", "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"music\终极.mp3");
        }
    }
}

using Milimoe.OneBot.Framework.Utility;

namespace Milimoe.RainBOT.Settings
{
    public class GeneralSettings
    {
        public static bool IsRun { get; set; } = true;

        public static long BotQQ { get; set; } = -1;

        public static long Master { get; set; } = -1;

        public static bool IsRepeat { get; set; } = true;

        public static long PRepeat { get; set; } = 7;

        public static int[] RepeatDelay { get; } = [30, 80];

        public static bool IsOSM { get; set; } = true;

        public static long POSM { get; set; } = 2;

        public static bool IsSayNo { get; set; } = true;

        public static long PSayNo { get; set; } = 16;

        public static bool IsMute { get; set; } = true;

        public static int[] MuteTime { get; } = [1200, 12600];

        public static bool IsReverseAt { get; set; } = true;

        public static long PReverseAt { get; set; } = 70;

        public static bool IsCallBrother { get; set; } = true;

        public static long PCallBrother { get; set; } = 4;

        public static bool IsDebug { get; set; } = false;

        public static long DebugGroupID { get; set; } = 0;

        public static long BlackTimes { get; set; } = 5;

        public static int BlackFrozenTime { get; set; } = 150;

        public static List<long> MuteAccessGroup { get; set; } = [];

        public static List<long> UnMuteAccessGroup { get; set; } = [];

        public static List<long> RecallAccessGroup { get; set; } = [];

        public static List<long> SayNoAccessGroup { get; set; } = [];

        public static List<long> OSMCoreGroup { get; set; } = [];

        public static PluginConfig Configs { get; set; } = new("rainbot", "config");

        public static void LoadSetting()
        {
            PluginConfig configs = new("rainbot", "config");
            configs.Load();
            if (configs.TryGetValue("BotQQ", out object? value) && value != null)
            {
                BotQQ = (long)value;
            }
            if (configs.TryGetValue("Master", out value) && value != null)
            {
                Master = (long)value;
            }
            if (configs.TryGetValue("IsRepeat", out value) && value != null)
            {
                IsRepeat = (bool)value;
            }
            if (configs.TryGetValue("PRepeat", out value) && value != null)
            {
                PRepeat = (long)value;
            }
            if (configs.TryGetValue("RepeatDelay", out value) && value != null)
            {
                long[] longs = [.. ((List<long>)value)];
                RepeatDelay[0] = Convert.ToInt32(longs[0]);
                RepeatDelay[1] = Convert.ToInt32(longs[1]);
            }
            if (configs.TryGetValue("IsOSM", out value) && value != null)
            {
                IsOSM = (bool)value;
            }
            if (configs.TryGetValue("POSM", out value) && value != null)
            {
                POSM = (long)value;
            }
            if (configs.TryGetValue("IsSayNo", out value) && value != null)
            {
                IsSayNo = (bool)value;
            }
            if (configs.TryGetValue("PSayNo", out value) && value != null)
            {
                PSayNo = (long)value;
            }
            if (configs.TryGetValue("IsMute", out value) && value != null)
            {
                IsMute = (bool)value;
            }
            if (configs.TryGetValue("MuteTime", out value) && value != null)
            {
                long[] longs = [.. ((List<long>)value)];
                MuteTime[0] = Convert.ToInt32(longs[0]);
                MuteTime[1] = Convert.ToInt32(longs[1]);
            }
            if (configs.TryGetValue("IsReverseAt", out value) && value != null)
            {
                IsReverseAt = (bool)value;
            }
            if (configs.TryGetValue("PReverseAt", out value) && value != null)
            {
                PReverseAt = (long)value;
            }
            if (configs.TryGetValue("IsCallBrother", out value) && value != null)
            {
                IsCallBrother = (bool)value;
            }
            if (configs.TryGetValue("PCallBrother", out value) && value != null)
            {
                PCallBrother = (long)value;
            }
            if (configs.TryGetValue("BlackTimes", out value) && value != null)
            {
                BlackTimes = (long)value;
            }
            if (configs.TryGetValue("BlackFrozenTime", out value) && value != null)
            {
                BlackFrozenTime = Convert.ToInt32((long)value);
            }
            if (configs.TryGetValue("MuteAccessGroup", out value) && value != null)
            {
                MuteAccessGroup = (List<long>)value;
            }
            if (configs.TryGetValue("UnMuteAccessGroup", out value) && value != null)
            {
                UnMuteAccessGroup = (List<long>)value;
            }
            if (configs.TryGetValue("RecallAccessGroup", out value) && value != null)
            {
                RecallAccessGroup = (List<long>)value;
            }
            if (configs.TryGetValue("SayNoAccessGroup", out value) && value != null)
            {
                SayNoAccessGroup = (List<long>)value;
            }
            if (configs.TryGetValue("OSMCoreGroup", out value) && value != null)
            {
                OSMCoreGroup = (List<long>)value;
            }
        }

        public static void SaveConfig()
        {
            Configs.Add("BotQQ", BotQQ);
            Configs.Add("Master", Master);
            Configs.Add("IsRepeat", IsRepeat);
            Configs.Add("PRepeat", PRepeat);
            Configs.Add("RepeatDelay", RepeatDelay);
            Configs.Add("IsOSM", IsOSM);
            Configs.Add("POSM", POSM);
            Configs.Add("IsSayNo", IsSayNo);
            Configs.Add("PSayNo", PSayNo);
            Configs.Add("IsMute", IsMute);
            Configs.Add("MuteTime", MuteTime);
            Configs.Add("IsReverseAt", IsReverseAt);
            Configs.Add("PReverseAt", PReverseAt);
            Configs.Add("IsCallBrother", IsCallBrother);
            Configs.Add("PCallBrother", PCallBrother);
            Configs.Add("BlackTimes", BlackTimes);
            Configs.Add("BlackFrozenTime", BlackFrozenTime);
            Configs.Add("MuteAccessGroup", MuteAccessGroup);
            Configs.Add("UnMuteAccessGroup", UnMuteAccessGroup);
            Configs.Add("RecallAccessGroup", RecallAccessGroup);
            Configs.Add("SayNoAccessGroup", SayNoAccessGroup);
            Configs.Add("OSMCoreGroup", OSMCoreGroup);
            Configs.Save();
        }

        public static void ShowAccessGroupMemberList(long target, string group, bool isgroup)
        {
            List<long> list = [];
            switch (group.ToLower())
            {
                case "muteaccessgroup":
                    list = MuteAccessGroup;
                    break;
                case "unmuteaccessgroup":
                    list = UnMuteAccessGroup;
                    break;
                case "recallaccessgroup":
                    list = RecallAccessGroup;
                    break;
                case "saynoaccessgroup":
                    list = SayNoAccessGroup;
                    break;
                case "osmcoregroup":
                    list = OSMCoreGroup;
                    break;
            }
            string msg = list.Count > 0 ? "权限组" + group + "拥有以下成员：" + "\r\n" + string.Join("\r\n", list) : "此权限组不存在或没有任何成员。";
            _ = isgroup ? Bot.SendGroupMessage(target, "显示列表成员", msg) : Bot.SendFriendMessage(target, "显示列表成员", msg);
        }
    }
}

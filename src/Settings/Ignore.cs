﻿using Milimoe.OneBot.Framework.Utility;

namespace Milimoe.RainBOT.Settings
{
    public class Ignore
    {
        public static HashSet<string> RepeatIgnore { get; set; } = [];

        public static List<long> CallBrotherQQIgnore { get; set; } = [];

        /// <summary>
        /// 这个属性暂时没用到 标记一下
        /// </summary>
        public static List<long> QQGroupIgnore { get; set; } = [];

        public static PluginConfig Configs { get; set; } = new("rainbot", "ignore");

        public static void InitIgnore()
        {
            PluginConfig configs = new("rainbot", "ignore");
            configs.Load();
            if (configs.TryGetValue("RepeatIgnore", out object? value) && value != null)
            {
                RepeatIgnore = new HashSet<string>((List<string>)value);
            }
            if (configs.TryGetValue("CallBrotherQQIgnore", out value) && value != null)
            {
                CallBrotherQQIgnore = new List<long>((List<long>)value);
            }
            if (configs.TryGetValue("QQGroupIgnore", out value) && value != null)
            {
                QQGroupIgnore = (List<long>)value;
            }
        }

        public static void SaveConfig()
        {
            Configs.Add("RepeatIgnore", RepeatIgnore);
            Configs.Add("CallBrotherQQIgnore", CallBrotherQQIgnore);
            Configs.Add("QQGroupIgnore", QQGroupIgnore);
            Configs.Save();
        }

        public static bool AddValue(string part, bool isadd, object value)
        {
            try
            {
                string strv = value.ToString() ?? "";
                switch (part.ToLower())
                {
                    case "repeatignore":
                        if (isadd) RepeatIgnore.Add(strv);
                        else RepeatIgnore.Remove(strv);
                        break;
                    case "callbrotherqqignore":
                        if (long.TryParse(strv, out long lv))
                        {
                            if (isadd) CallBrotherQQIgnore.Add(lv);
                            else CallBrotherQQIgnore.Remove(lv);
                        }
                        break;
                    case "qqgroupignore":
                        if (long.TryParse(strv, out lv))
                        {
                            if (isadd) QQGroupIgnore.Add(lv);
                            else QQGroupIgnore.Remove(lv);
                        }
                        break;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static void ShowList(long target, string group, bool isgroup)
        {
            List<string> list = [];
            switch (group.ToLower())
            {
                case "repeatignore":
                    list = [..RepeatIgnore];
                    break;
                case "callbrotherqqignore":
                    list = CallBrotherQQIgnore.Select(x => x.ToString()).ToList();
                    break;
                case "qqgroupignore":
                    list = QQGroupIgnore.Select(x => x.ToString()).ToList();
                    break;
            }
            string msg = list.Count > 0 ? "列表" + group + "拥有以下成员：" + "\r\n" + string.Join("\r\n", list) : "此列表不存在或没有任何成员。";
            _ = isgroup ? Bot.SendGroupMessage(target, "显示列表成员", msg) : Bot.SendFriendMessage(target, "显示列表成员", msg);
        }
    }
}

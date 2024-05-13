using System.Text.RegularExpressions;
using Milimoe.OneBot.Framework;
using Milimoe.OneBot.Framework.Utility;
using Milimoe.OneBot.Model.Content;
using Milimoe.OneBot.Model.Message;
using Milimoe.RainBOT.Settings;

namespace Milimoe.RainBOT.Command
{
    public class MasterCommand
    {
        public static void Execute(string command, long user_id, bool onOSMCore, long target_id, bool send_group)
        {
            // OSM指令
            if (GeneralSettings.IsRun && command.Contains(".osm info"))
            {
                // OSM核心状态
                string msg = "OSM插件运行状态：";
                if ((send_group && onOSMCore) || !send_group)
                {
                    if (onOSMCore)
                    {
                        msg += "\r\n本群已启用OSM核心";
                    }
                    if (GeneralSettings.IsRepeat)
                    {
                        msg += "\r\n随机复读：开启";
                        msg += $"\r\n随机复读概率：{GeneralSettings.PRepeat}%" +
                            $"\r\n随机复读延迟区间：{GeneralSettings.RepeatDelay[0]}至{GeneralSettings.RepeatDelay[1]}秒";
                    }
                    else msg += "\r\n随机复读：关闭";
                    if (GeneralSettings.IsOSM)
                    {
                        msg += "\r\n随机OSM：开启";
                        msg += $"\r\n随机OSM概率：{GeneralSettings.POSM}%";
                    }
                    else msg += "\r\n随机OSM：关闭";
                    if (GeneralSettings.IsSayNo)
                    {
                        msg += "\r\n随机反驳不：开启";
                        msg += $"\r\n随机反驳不概率：{GeneralSettings.PSayNo}%";
                    }
                    else msg += "\r\n随机反驳不：关闭";
                    if (GeneralSettings.IsMute)
                    {
                        msg += "\r\n禁言抽奖：开启";
                        msg += $"\r\n禁言抽奖时长区间：{GeneralSettings.MuteTime[0]}至{GeneralSettings.MuteTime[1]}秒";
                    }
                    else msg += "\r\n禁言抽奖：关闭";
                }
                else
                {
                    msg += "\r\n本群未启用OSM核心";
                }
                SendMessage(send_group, target_id, msg);
            }
            else if (GeneralSettings.IsRun && command.Contains(".osm stop"))
            {
                if (user_id == GeneralSettings.Master)
                {
                    string result = Execute_Worker(".osm stop", "");
                    SendMessage(send_group, target_id, result);
                }
                else Access_Denied(send_group, target_id);
            }
            else if (!GeneralSettings.IsRun && command.Contains(".osm start"))
            {
                if (user_id == GeneralSettings.Master)
                {
                    string result = Execute_Worker(".osm start", "");
                    SendMessage(send_group, target_id, result);
                }
                else Access_Denied(send_group, target_id);
            }
            else if ((command.Contains(".osm set admin") || command.Contains(".osm set unadmin")) && send_group)
            {
                if (user_id == GeneralSettings.Master)
                {
                    bool enable = !command.Contains("unadmin");
                    string str = command.Replace(".osm set admin", "").Replace(".osm set unadmin", "").Trim();
                    string[] strs = Regex.Split(str, @"\s+");
                    if (strs.Length > 0)
                    {
                        str = strs[0].Replace(@"@", "").Trim();
                        if (long.TryParse(str, out long qq))
                        {
                            SetGroupAdminContent content = new(target_id, qq, enable);
                            _ = Bot.SendMessage(SupportedAPI.set_group_admin, target_id, "OSM指令", content, send_group);
                        }
                        else _ = Bot.SendGroupMessage(target_id, "OSM指令", Execute_Worker(".osm set", "admin", strs.Length > 1 ? strs[1..] : []));
                    }
                    else _ = Bot.SendGroupMessage(target_id, "OSM指令", Execute_Worker(".osm set", "admin", strs.Length > 1 ? strs[1..] : []));
                }
                else Access_Denied(send_group, target_id);
            }
            else if (command.Contains(".osm mutegroup"))
            {
                TaskUtility.NewTask(async () => await Bot.MuteGroup(user_id, target_id, command));
            }
            else if (command.Contains(".osm mute") && send_group)
            {
                TaskUtility.NewTask(async () => await Bot.Mute(user_id, target_id, command));
            }
            else if (command.Contains(".osm refresh"))
            {
                if (user_id == GeneralSettings.Master)
                {
                    TaskUtility.NewTask(async () =>
                    {
                        await Bot.GetGroups();
                        await Bot.GetGroupMembers();
                        SendMessage(send_group, target_id, "刷新缓存完成。请注意，刷新缓存会导致正在禁言中的成员无法通过私聊忏悔命令解禁。");
                    });
                }
                else Access_Denied(send_group, target_id);
            }
            else if (command.Contains(".osm reload"))
            {
                if (user_id == GeneralSettings.Master)
                {
                    GeneralSettings.LoadSetting();
                    Music.InitMusicList();
                    Daily.InitDaily();
                    SayNo.InitSayNo();
                    Ignore.InitIgnore();
                    SendMessage(send_group, target_id, "参数设定以及权限组重新加载完成。");
                }
                else Access_Denied(send_group, target_id);
            }
            else if (command.Contains(".osm set"))
            {
                if (user_id == GeneralSettings.Master)
                {
                    string str = command.Replace(".osm set", "").Trim();
                    string[] strs = Regex.Split(str, @"\s+");
                    string result = Execute_Worker(".osm set", strs[0], strs.Length > 1 ? strs[1..] : []);
                    SendMessage(send_group, target_id, result);
                }
                else Access_Denied(send_group, target_id);
            }
            else if (command.Contains(".osm sendall"))
            {
                TaskUtility.NewTask(async () =>
                {
                    if (user_id == GeneralSettings.Master)
                    {
                        string str = command.Replace(".osm sendall", "").Trim();
                        foreach (long group_id in Bot.Groups.Select(g => g.group_id))
                        {
                            await Bot.SendGroupMessage(group_id, "OSM指令", str);
                        }
                    }
                    else Access_Denied(send_group, target_id);
                });
            }
            else if (command.Contains(".osm send"))
            {
                TaskUtility.NewTask(async () =>
                {
                    if (user_id == GeneralSettings.Master)
                    {
                        string str = command.Replace(".osm send", "").Trim();
                        string[] strs = Regex.Split(str, @"\s+");
                        if (long.TryParse(strs[0], out long group_id) && strs.Length > 1)
                        {
                            await Bot.SendGroupMessage(group_id, "OSM指令", string.Join(" ", strs[1..]));
                        }
                    }
                    else Access_Denied(send_group, target_id);
                });
            }
            else if (command.Contains(".osm core") || command == ".osm")
            {
                // OSM核心信息
                if (send_group)
                {
                    GroupMessageContent content = new(target_id);
                    content.message.Add(new TextMessage(OSMCore.Info));
                    content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\raincandy.jpg"));
                    _ = Bot.SendGroupMessage(target_id, "OSM核心", content);
                }
                else
                {
                    FriendMessageContent content = new(target_id);
                    content.message.Add(new TextMessage(OSMCore.Info));
                    content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\raincandy.jpg"));
                    _ = Bot.SendFriendMessage(target_id, "OSM核心", content);
                }
                return;
            }
            else
            {
                SendMessage(send_group, target_id, Execute_Worker(".osm missingcommand", ""));
            }
            return;
        }

        public static string Execute_Worker(string command, string part, params string[] args)
        {
            string msg;
            bool isadd;
            command = command.ToLower();
            part = part.ToLower();
            switch (command)
            {
                case ".osm stop":
                    GeneralSettings.IsRun = false;
                    return "OSM Core：服务已关闭。";
                case ".osm start":
                    GeneralSettings.IsRun = true;
                    return "OSM Core：服务已启动。";
                case ".osm set":
                    bool status;
                    switch (part)
                    {
                        case "repeat":
                            if (args.Length > 0)
                            {
                                if (args[0] != "on" && args[0] != "off")
                                {
                                    break;
                                }
                                status = args[0] == "on";
                                msg = UpdateValue("随机复读", GeneralSettings.IsRepeat ? "开启" : "关闭", status ? "开启" : "关闭");
                                GeneralSettings.IsRepeat = status;
                                return msg;
                            }
                            break;
                        case "prepeat":
                            if (args.Length > 0 && int.TryParse(args[0], out int prepeat))
                            {
                                if (prepeat >= 0 && prepeat <= 100)
                                {
                                    msg = UpdateValue("随机复读的概率", GeneralSettings.PRepeat + "%", prepeat + "%");
                                    GeneralSettings.PRepeat = prepeat;
                                    return msg;
                                }
                            }
                            break;
                        case "mindelay":
                            if (args.Length > 0 && int.TryParse(args[0], out int mindelay))
                            {
                                if (mindelay > 0 && mindelay <= 600 && mindelay <= GeneralSettings.RepeatDelay[1])
                                {
                                    msg = UpdateValue("最短复读延迟", GeneralSettings.RepeatDelay[0] + "秒", mindelay + "秒");
                                    GeneralSettings.RepeatDelay[0] = mindelay;
                                    return msg;
                                }
                                return "OSM Core：最短复读延迟不能小于等于0或者大于最长复读延迟（" + GeneralSettings.RepeatDelay[1] + "秒），不能大于600秒（10分钟）。";
                            }
                            break;
                        case "maxdelay":
                            if (args.Length > 0 && int.TryParse(args[0], out int maxdelay))
                            {
                                if (maxdelay >= 0 && maxdelay <= 600 && maxdelay >= GeneralSettings.RepeatDelay[0])
                                {
                                    msg = UpdateValue("最长复读延迟", GeneralSettings.RepeatDelay[1] + "秒", maxdelay + "秒");
                                    GeneralSettings.RepeatDelay[1] = maxdelay;
                                    return msg;
                                }
                                return "OSM Core：最长复读延迟不能小于等于0或者小于最短复读延迟（" + GeneralSettings.RepeatDelay[0] + "秒），不能大于600秒（10分钟）。";
                            }
                            break;
                        case "osm":
                            if (args.Length > 0)
                            {
                                if (args[0] != "on" && args[0] != "off")
                                {
                                    break;
                                }
                                status = args[0] == "on";
                                msg = UpdateValue("随机OSM", GeneralSettings.IsOSM ? "开启" : "关闭", status ? "开启" : "关闭");
                                GeneralSettings.IsOSM = status;
                                return msg;
                            }
                            break;
                        case "posm":
                            if (args.Length > 0 && int.TryParse(args[0], out int posm))
                            {
                                if (posm >= 0 && posm <= 100)
                                {
                                    msg = UpdateValue("随机OSM的概率", GeneralSettings.POSM + "%", posm + "%");
                                    GeneralSettings.POSM = posm;
                                    return msg;
                                }
                            }
                            break;
                        case "sayno":
                            if (args.Length > 0)
                            {
                                if (args[0] != "on" && args[0] != "off")
                                {
                                    break;
                                }
                                status = args[0] == "on";
                                msg = UpdateValue("随机反驳不", GeneralSettings.IsSayNo ? "开启" : "关闭", status ? "开启" : "关闭");
                                GeneralSettings.IsSayNo = status;
                                return msg;
                            }
                            break;
                        case "psayno":
                            if (args.Length > 0 && int.TryParse(args[0], out int psayno))
                            {
                                if (psayno >= 0 && psayno <= 100)
                                {
                                    msg = UpdateValue("随机反驳不的概率", GeneralSettings.PSayNo + "%", psayno + "%");
                                    GeneralSettings.PSayNo = psayno;
                                    return msg;
                                }
                            }
                            break;
                        case "reverseat":
                            if (args.Length > 0)
                            {
                                if (args[0] != "on" && args[0] != "off")
                                {
                                    break;
                                }
                                status = args[0] == "on";
                                msg = UpdateValue("反向艾特", GeneralSettings.IsReverseAt ? "开启" : "关闭", status ? "开启" : "关闭");
                                GeneralSettings.IsReverseAt = status;
                                return msg;
                            }
                            break;
                        case "preverseat":
                            if (args.Length > 0 && int.TryParse(args[0], out int preverseat))
                            {
                                if (preverseat >= 0 && preverseat <= 100)
                                {
                                    msg = UpdateValue("反向艾特的概率", GeneralSettings.PReverseAt + "%", preverseat + "%");
                                    GeneralSettings.PReverseAt = preverseat;
                                    return msg;
                                }
                            }
                            break;
                        case "callbrother":
                            if (args.Length > 0)
                            {
                                if (args[0] != "on" && args[0] != "off")
                                {
                                    break;
                                }
                                status = args[0] == "on";
                                msg = UpdateValue("随机叫哥", GeneralSettings.IsCallBrother ? "开启" : "关闭", status ? "开启" : "关闭");
                                GeneralSettings.IsCallBrother = status;
                                return msg;
                            }
                            break;
                        case "pcallbrother":
                            if (args.Length > 0 && int.TryParse(args[0], out int pcallbrother))
                            {
                                if (pcallbrother >= 0 && pcallbrother <= 100)
                                {
                                    msg = UpdateValue("随机叫哥的概率", GeneralSettings.PCallBrother + "%", pcallbrother + "%");
                                    GeneralSettings.PCallBrother = pcallbrother;
                                    return msg;
                                }
                            }
                            break;
                        case "mute":
                            if (args.Length > 0)
                            {
                                if (args[0] != "on" && args[0] != "off")
                                {
                                    break;
                                }
                                status = args[0] == "on";
                                msg = UpdateValue("禁言抽奖", GeneralSettings.IsMute ? "开启" : "关闭", status ? "开启" : "关闭");
                                GeneralSettings.IsMute = status;
                                return msg;
                            }
                            break;
                        case "minmute":
                            if (args.Length > 0 && int.TryParse(args[0], out int minmute))
                            {
                                if (minmute > 0 && minmute <= 604800 && minmute <= GeneralSettings.MuteTime[1])
                                {
                                    msg = UpdateValue("最短禁言时长", GeneralSettings.MuteTime[0] + "秒", minmute + "秒");
                                    GeneralSettings.MuteTime[0] = minmute;
                                    return msg;
                                }
                                return "OSM Core：最短禁言时长不能小于等于0或者大于最长禁言时长（" + GeneralSettings.MuteTime[1] + "秒），不能大于604800秒（7天）。";
                            }
                            break;
                        case "maxmute":
                            if (args.Length > 0 && int.TryParse(args[0], out int maxmute))
                            {
                                if (maxmute >= 0 && maxmute <= 604800 && maxmute >= GeneralSettings.MuteTime[0])
                                {
                                    msg = UpdateValue("最长禁言时长", GeneralSettings.MuteTime[1] + "秒", maxmute + "秒");
                                    GeneralSettings.MuteTime[1] = maxmute;
                                    return msg;
                                }
                                return "OSM Core：最长禁言时长不能小于等于0或者小于最短禁言时长（" + GeneralSettings.MuteTime[0] + "秒），不能大于604800秒（7天）。";
                            }
                            break;
                        case "blacktimes":
                            if (args.Length > 0 && int.TryParse(args[0], out int blacktimes))
                            {
                                if (blacktimes >= 0 && blacktimes <= 100)
                                {
                                    msg = UpdateValue("每分钟频繁操作上限次数", GeneralSettings.BlackTimes + "次", blacktimes + "次");
                                    GeneralSettings.BlackTimes = blacktimes;
                                    return msg;
                                }
                            }
                            break;
                        case "frozentime":
                            if (args.Length > 0 && int.TryParse(args[0], out int blackfrozentime))
                            {
                                if (blackfrozentime >= 0 && blackfrozentime <= 100)
                                {
                                    msg = UpdateValue("频繁操作封禁时间", GeneralSettings.BlackFrozenTime + "秒", blackfrozentime + "秒");
                                    GeneralSettings.BlackFrozenTime = blackfrozentime;
                                    return msg;
                                }
                            }
                            break;
                        case "muteaccessgroup":
                            if (args.Length > 1 && long.TryParse(args[1].Replace("@", "").Trim(), out long muteaccess_qq))
                            {
                                string args0 = args[0].ToString().Trim();
                                if (args0 == "add" || args0 == "remove" || args0 == "+" || args0 == "-")
                                {
                                    isadd = args0 == "add" || args0 == "+";
                                    if (isadd) GeneralSettings.MuteAccessGroup.Add(muteaccess_qq);
                                    else GeneralSettings.MuteAccessGroup.Remove(muteaccess_qq);
                                    msg = AddRemoveAccessGroupMember("禁言权限组成员", isadd, muteaccess_qq);
                                    return msg;
                                }
                            }
                            break;
                        case "unmuteaccessgroup":
                            if (args.Length > 1 && long.TryParse(args[1].Replace("@", "").Trim(), out long unmuteaccess_qq))
                            {
                                string args0 = args[0].ToString().Trim();
                                if (args0 == "add" || args0 == "remove" || args0 == "+" || args0 == "-")
                                {
                                    isadd = args0 == "add" || args0 == "+";
                                    if (isadd) GeneralSettings.UnMuteAccessGroup.Add(unmuteaccess_qq);
                                    else GeneralSettings.UnMuteAccessGroup.Remove(unmuteaccess_qq);
                                    msg = AddRemoveAccessGroupMember("解禁权限组成员", isadd, unmuteaccess_qq);
                                    return msg;
                                }
                            }
                            break;
                        case "recallaccessgroup":
                            if (args.Length > 1 && long.TryParse(args[1].Replace("@", "").Trim(), out long recallaccess_qq))
                            {
                                string args0 = args[0].ToString().Trim();
                                if (args0 == "add" || args0 == "remove" || args0 == "+" || args0 == "-")
                                {
                                    isadd = args0 == "add" || args0 == "+";
                                    if (isadd) GeneralSettings.RecallAccessGroup.Add(recallaccess_qq);
                                    else GeneralSettings.RecallAccessGroup.Remove(recallaccess_qq);
                                    msg = AddRemoveAccessGroupMember("撤回权限组成员", isadd, recallaccess_qq);
                                    return msg;
                                }
                            }
                            break;
                        case "osmcoregroup":
                            if (args.Length > 1 && long.TryParse(args[1].Trim(), out long osmcoregroup_id))
                            {
                                string args0 = args[0].ToString().Trim();
                                if (args0 == "add" || args0 == "remove" || args0 == "+" || args0 == "-")
                                {
                                    isadd = args0 == "add" || args0 == "+";
                                    if (isadd) GeneralSettings.OSMCoreGroup.Add(osmcoregroup_id);
                                    else GeneralSettings.OSMCoreGroup.Remove(osmcoregroup_id);
                                    msg = AddRemoveAccessGroupMember("OSM核心启用群聊", isadd, osmcoregroup_id);
                                    return msg;
                                }
                            }
                            break;
                    }
                    return "OSM Core：指令格式不正确或传入的参数不支持。\r\n格式：.osm <command> [part] [args...]";
            }
            return "OSM Core：指令格式不正确或传入的参数不支持。\r\n格式：.osm <command> [part] [args...]";
        }

        public static void Access_Denied(bool send_group, long target_id) => SendMessage(send_group, target_id, "你没有权限使用此指令。");
        
        public static void SendMessage(bool send_group, long target_id, string msg)
        {
            _ = send_group ? Bot.SendGroupMessage(target_id, "OSM指令", msg) : Bot.SendFriendMessage(target_id, "OSM指令", msg);
        }

        public static string UpdateValue(string part, string old_value, string new_value, ConsoleColor color = ConsoleColor.Cyan)
        {
            string msg = "OSM Core：" + part + "已调整为：" + new_value + "（原始值：" + old_value + "）。";
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.Gray;
            GeneralSettings.SaveConfig();
            return msg;
        }

        public static string AddRemoveAccessGroupMember(string part, bool isadd, long value, ConsoleColor color = ConsoleColor.Cyan)
        {
            string msg = "OSM Core：" + part + $"已{(isadd ? "添加" : "移除")}：" + value + "。";
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.Gray;
            GeneralSettings.SaveConfig();
            return msg;
        }
    }
}

using System.Linq;
using System.Text.RegularExpressions;
using Milimoe.OneBot.Framework;
using Milimoe.OneBot.Framework.Interface;
using Milimoe.OneBot.Framework.Utility;
using Milimoe.OneBot.Model.Content;
using Milimoe.OneBot.Model.Event;
using Milimoe.OneBot.Model.Message;
using Milimoe.OneBot.Model.Other;
using Milimoe.OneBot.Model.QuickReply;
using Milimoe.OneBot.Utility;
using Milimoe.RainBOT.Command;
using Milimoe.RainBOT.Settings;

namespace Milimoe.RainBOT.ListeningTask
{
    public class GroupMessageTask
    {
        private static long dice = 0;
        private readonly static string[] EEWords = ["ee", "鹅鹅", "呃呃", "谔谔", "饿饿"];
        private readonly static string[] MuteCommands = ["禁言", "解禁"];

        public static void ListeningTask_handler(GroupMessageEvent e, out GroupMsgEventQuickReply? quick_reply)
        {
            quick_reply = null;
            try
            {
                Sender sender = e.sender;
                if (e.user_id == 0 || e.sender.user_id == 0) return;

                Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} G/{e.group_id}{(e.detail.Trim() == "" ? "" : " -> " + e.detail)} by {sender.user_id}（{(sender.card != "" ? sender.card : sender.nickname)}）");
                if (GeneralSettings.IsDebug)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"DEBUG：{e.original_msg}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                if (MuteRecall.Muted.TryGetValue(e.group_id, out Dictionary<long, long>? mute_group) && mute_group != null) mute_group.Remove(e.user_id);

                bool onOSMCore = GeneralSettings.OSMCoreGroup.Contains(e.group_id);
                // OSM指令
                if (e.detail.Length >= 4 && e.detail[..4] == ".osm")
                {
                    if (GeneralSettings.IsRun && e.detail.Contains(".osm info"))
                    {
                        // OSM核心状态
                        string msg = "OSM插件运行状态：";
                        if (onOSMCore)
                        {
                            msg += "\r\n本群已启用OSM核心";
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
                        _ = Post(e, "OSM状态", msg);
                    }
                    else if (GeneralSettings.IsRun && e.detail.Contains(".osm stop"))
                    {
                        if (e.user_id == GeneralSettings.Master)
                        {
                            string result = MasterCommand.Execute(".osm stop", "");
                            _ = Post(e, "OSM指令", result);
                        }
                        else _ = Post(e, "OSM指令", "你没有权限使用此指令。");
                    }
                    else if (!GeneralSettings.IsRun && e.detail.Contains(".osm start"))
                    {
                        if (e.user_id == GeneralSettings.Master)
                        {
                            string result = MasterCommand.Execute(".osm start", "");
                            _ = Post(e, "OSM指令", result);
                        }
                        else _ = Post(e, "OSM指令", "你没有权限使用此指令。");
                    }
                    else if (e.detail.Contains(".osm set admin") || e.detail.Contains(".osm set unadmin"))
                    {
                        if (e.user_id == GeneralSettings.Master)
                        {
                            bool enable = !e.detail.Contains("unadmin");
                            string str = e.detail.Replace(".osm set admin", "").Replace(".osm set unadmin", "").Trim();
                            string[] strs = Regex.Split(str, @"\s+");
                            if (strs.Length > 0)
                            {
                                str = strs[0].Replace(@"@", "").Trim();
                                if (long.TryParse(str, out long qq))
                                {
                                    SetGroupAdminContent content = new(e.group_id, qq, enable);
                                    _ = Post(SupportedAPI.set_group_admin, e.group_id, "OSM指令", content);
                                }
                                else _ = Post(e, "OSM指令", MasterCommand.Execute(".osm set", "admin", strs.Length > 1 ? strs[1..] : []));
                            }
                            else _ = Post(e, "OSM指令", MasterCommand.Execute(".osm set", "admin", strs.Length > 1 ? strs[1..] : []));
                        }
                        else _ = Post(e, "OSM指令", "你没有权限使用此指令。");
                    }
                    else if (e.detail.Contains(".osm mutegroup"))
                    {
                        TaskUtility.NewTask(async () => await MuteGroup(e));
                    }
                    else if (e.detail.Contains(".osm mute"))
                    {
                        TaskUtility.NewTask(async () => await Mute(e));
                    }
                    else if (e.detail.Contains(".osm refresh"))
                    {
                        if (e.user_id == GeneralSettings.Master)
                        {
                            TaskUtility.NewTask(async () =>
                            {
                                await Bot.GetGroups();
                                await Bot.GetGroupMembers();
                                await Post(e, "OSM指令", "刷新缓存完成。请注意，刷新缓存会导致正在禁言中的成员无法通过私聊忏悔命令解禁。");
                            });
                        }
                        else _ = Post(e, "OSM指令", "你没有权限使用此指令。");
                    }
                    else if (e.detail.Contains(".osm reload"))
                    {
                        if (e.user_id == GeneralSettings.Master)
                        {
                            GeneralSettings.LoadSetting();
                            _ = Post(e, "OSM指令", "参数设定以及权限组重新加载完成。");
                        }
                        else _ = Post(e, "OSM指令", "你没有权限使用此指令。");
                    }
                    else if (e.detail.Contains(".osm set"))
                    {
                        if (e.user_id == GeneralSettings.Master)
                        {
                            string str = e.detail.Replace(".osm set", "").Trim();
                            string[] strs = Regex.Split(str, @"\s+");
                            string result = MasterCommand.Execute(".osm set", strs[0], strs.Length > 1 ? strs[1..] : []);
                            _ = Post(e, "OSM指令", result);
                        }
                        else _ = Post(e, "OSM指令", "你没有权限使用此指令。");
                    }
                    else if (e.detail.Contains(".osm sendall"))
                    {
                        TaskUtility.NewTask(async () =>
                        {
                            if (e.user_id == GeneralSettings.Master)
                            {
                                string str = e.detail.Replace(".osm sendall", "").Trim();
                                string result = MasterCommand.Execute(".osm sendall", str);
                                foreach (long group_id in Bot.Groups.Select(g => g.group_id))
                                {
                                    GroupMessageContent content = new(group_id);
                                    content.message.Add(new TextMessage(result));
                                    await Post(SupportedAPI.send_group_msg, group_id, "OSM指令", content);
                                }
                            }
                            else await Post(e, "OSM指令", "你没有权限使用此指令。");
                        });
                    }
                    else if (e.detail.Contains(".osm send"))
                    {
                        TaskUtility.NewTask(async () =>
                        {
                            if (e.user_id == GeneralSettings.Master)
                            {
                                string str = e.detail.Replace(".osm send", "").Trim();
                                string[] strs = Regex.Split(str, @"\s+");
                                string result = MasterCommand.Execute(".osm send", strs[0], str.Replace(strs[0], "").Trim());
                                if (long.TryParse(strs[0], out long group_id))
                                {
                                    GroupMessageContent content = new(group_id);
                                    content.message.Add(new TextMessage(result));
                                    await Post(SupportedAPI.send_group_msg, group_id, "OSM指令", content);
                                }
                            }
                            else await Post(e, "OSM指令", "你没有权限使用此指令。");
                        });
                    }
                    else
                    {
                        // OSM核心信息
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new TextMessage(OSMCore.Info));
                        content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\raincandy.jpg"));
                        _ = Post(e, "OSM核心", content);
                        return;
                    }
                    return;
                }

                if (e.detail.Length >= 5 && (e.detail[..5] == "禁言所有人" || e.detail[..5] == "解禁所有人") && e.user_id == GeneralSettings.Master && Bot.GroupMembers.TryGetValue(e.group_id, out List<Member>? members) && members != null)
                {
                    TaskUtility.NewTask(async () => await Mute(e, members.Where(m => m.user_id != GeneralSettings.Master).Select(m => m.user_id)));
                    return;
                }

                if (e.detail != "禁言抽奖" && e.detail.Length >= 2 && MuteCommands.Any(e.detail[..2].Contains))
                {
                    TaskUtility.NewTask(async () => await Mute(e));
                    return;
                }

                if (e.detail.Length >= 4 && e.detail[..4] == "跨群禁言")
                {
                    TaskUtility.NewTask(async () => await MuteGroup(e));
                    return;
                }

                // 撤回消息
                if ((e.user_id == GeneralSettings.Master || GeneralSettings.RecallAccessGroup.Contains(e.user_id)) && e.detail.Contains("撤回；") && e.message.Any(m => m.type == "reply"))
                {
                    ReplyMessage reply = (ReplyMessage)e.message.Where(m => m.type == "reply").First();
                    if (int.TryParse(reply.data.id, out int id))
                    {
                        TaskUtility.NewTask(async () => await Post(SupportedAPI.delete_msg, e.group_id, "撤回", new DeleteMsgContent(id)));
                        TaskUtility.NewTask(async () => await Post(SupportedAPI.delete_msg, e.group_id, "撤回", new DeleteMsgContent(e.real_id)));
                        return;
                    }
                }

                if (!GeneralSettings.IsRun)
                {
                    return;
                }

                // 发图API
                if (e.detail == "来图")
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("https://iw233.cn/api.php?sort=random"));
                        await Post(e, "Image", content);
                        return;
                    });
                }
                if (e.detail.Contains("白毛"))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("https://iw233.cn/api.php?sort=yin"));
                        await Post(e, "Image", content);
                        return;
                    });
                }
                if (e.detail == "猫耳")
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("https://iw233.cn/api.php?sort=cat"));
                        await Post(e, "Image", content);
                        return;
                    });
                }
                if (e.detail == "壁纸")
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("https://iw233.cn/api.php?sort=pc"));
                        await Post(e, "Image", content);
                        return;
                    });
                }
                if (e.detail == "新闻")
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("https://api.03c3.cn/api/zb"));
                        await Post(e, "Image", content);
                        return;
                    });
                }
                if (e.detail.Contains("来龙"))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\long\long (" + (new Random().Next(1540) + 1) + ").jpg"));
                        await Post(e, "Image", content);
                        return;
                    });
                }
                if (e.detail.Contains("丁真") || e.detail == "一眼丁真" || e.detail == "一眼顶针")
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\dingzhen\dz" + (new Random().Next(82) + 1) + ".jpg"));
                        await Post(e, "Image", content);
                        return;
                    });
                }
                if (EEWords.Any(e.detail.Contains))
                {
                    if (BlackList.Times.TryGetValue(e.user_id, out long bltimes) && bltimes > 5) return;
                    GroupMessageContent content = new(e.group_id);
                    content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\ee.png"));
                    _ = Post(e, "Image", content);
                    return;
                }

                // 发音频API
                if (e.detail.Contains("kun", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["ikun"]));
                        await Post(e, "Record", content);
                    });
                    return;
                }
                if (e.detail.Contains("csgo", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["懂CSGO"]));
                        await Post(e, "Record", content);
                    });
                    return;
                }
                if (e.detail.Contains("架不住") || e.detail.Contains("打不死") || e.detail.Contains("不玩了"))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["令人沮丧的游戏"]));
                        await Post(e, "Record", content);
                    });
                }
                if (e.detail.Contains("man", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["man"]));
                        await Post(e, "Record", content);
                    });
                    return;
                }
                if (e.detail.Contains("马云", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["马云"]));
                        await Post(e, "Record", content);
                    });
                    return;
                }
                if (e.detail.Contains("电锯", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["电锯"]));
                        await Post(e, "Record", content);
                    });
                    return;
                }
                if (e.detail.Contains("疤王", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["疤王"]));
                        await Post(e, "Record", content);
                    });
                    return;
                }
                if (e.detail.Contains("终极", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["终极"]));
                        await Post(e, "Record", content);
                    });
                    return;
                }
                if (e.detail.Contains("音乐", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList[Music.MusicList.Keys.ToArray()[new Random().Next(Music.MusicList.Count)]]));
                        await Post(e, "Record", content);
                    });
                    return;
                }

                // 我的运势
                if (e.detail == "我的运势")
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new AtMessage(e.user_id));
                        if (Daily.UserDailys.TryGetValue(e.user_id, out string? value) && value != null && value.Trim() != "")
                        {
                            content.message.Add(new TextMessage("你已看过你的今日运势：\r\n"));
                            content.message.Add(new TextMessage(value));
                            await Post(e, "我的运势", content);
                        }
                        else
                        {
                            int seq = new Random().Next(Daily.DailyContent.Count);
                            string text = Daily.DailyContent[seq];
                            Daily.UserDailys.Add(e.user_id, text);
                            content.message.Add(new TextMessage("你的今日运势是：\r\n" + text));
                            await Post(e, "我的运势", content);
                            // 配图
                            content = new(e.group_id);
                            string img = "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\zi\";
                            if (seq >= 0 && seq <= 5)
                            {
                                img += "dj" + (new Random().Next(3) + 1) + ".png";
                            }
                            else if (seq >= 6 && seq <= 10)
                            {
                                img += "zj" + (new Random().Next(2) + 1) + ".png";
                            }
                            else if (seq >= 11 && seq <= 15)
                            {
                                img += "j" + (new Random().Next(4) + 1) + ".png";
                            }
                            else if (seq >= 16 && seq <= 22)
                            {
                                img += "mj" + (new Random().Next(2) + 1) + ".png";
                            }
                            else if (seq >= 23 && seq <= 25)
                            {
                                img += "dx" + (new Random().Next(2) + 1) + ".png";
                            }
                            else if (seq >= 26 && seq <= 29)
                            {
                                img += "x" + (new Random().Next(2) + 1) + ".png";
                            }
                            content.message.Add(new ImageMessage(img));
                            await Post(e, "我的运势配图", content);
                        }
                    });
                    return;
                }
                if (e.detail == "重置运势" && Daily.UserDailys.ContainsKey(e.user_id))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        Daily.UserDailys.Remove(e.user_id);
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new AtMessage(e.user_id));
                        content.message.Add(new TextMessage("你的今日运势已重置。"));
                        await Post(e, "重置运势", content);
                    });
                    return;
                }
                if (e.detail.Length > 4 && e.detail[..2] == "查看" && (e.detail[^2..] == "运势"))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        string[] strs = e.detail.Replace("查看", "").Replace("运势", "").Trim().Split(' ');
                        foreach (string str_qq in strs)
                        {
                            if (long.TryParse(str_qq.Trim().Replace("@", ""), out long qq))
                            {
                                if (qq == GeneralSettings.BotQQ && !Daily.UserDailys.ContainsKey(qq))
                                {
                                    string text = Daily.DailyContent[new Random().Next(Daily.DailyContent.Count)];
                                    Daily.UserDailys.Add(GeneralSettings.BotQQ, text);
                                }
                                if (Daily.UserDailys.TryGetValue(qq, out string? daily) && daily != null)
                                {
                                    GroupMessageContent content = new(e.group_id);
                                    content.message.Add(new TextMessage(qq + "（" + Bot.GetMemberNickName(e.group_id, e.user_id) + "）的今日运势是：\r\n" + daily));
                                    await Post(e, "查看运势", content);
                                }
                                else
                                {
                                    await Post(e, "查看运势", "TA今天还没有抽取运势哦，快去提醒TA！");
                                }
                            }
                        }
                    });
                    return;
                }

                // 下面是开启了OSM Core的群组才能使用的功能
                if (!onOSMCore) return;

                // 禁言抽奖
                if (GeneralSettings.IsMute && e.detail == "禁言抽奖" && !MuteRecall.WillMute.ContainsKey(e.user_id))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e) || !Bot.BotIsAdmin(e.group_id)) return;
                        if (e.user_id != GeneralSettings.Master)
                        {
                            await Post(e, "禁言抽奖", "2秒后开奖～\r\n如需要忏悔，请在开奖后3秒内发送忏悔，开奖前发送无效。");
                            await Task.Delay(2000);
                            if (!MuteRecall.WillMute.ContainsKey(e.user_id)) MuteRecall.WillMute.Add(e.user_id, e.user_id);
                            long mute_time = GeneralSettings.MuteTime[0] + new Random().NextInt64(GeneralSettings.MuteTime[1] - GeneralSettings.MuteTime[0]);
                            await Post(e, "禁言抽奖", "开奖啦！禁言时长：" + (mute_time / 60) + "分钟" + (mute_time % 60) + "秒。\r\n" + "你现在有3秒时间发送忏悔拒绝领奖！");
                            await Task.Delay(3200);
                            await Post(SupportedAPI.set_group_ban, e.group_id, "禁言抽奖", new SetGroupBanContent(e.group_id, e.user_id, mute_time));
                            MuteRecall.WillMute.Remove(e.user_id);
                        }
                        else
                        {
                            _ = Post(e, "禁言抽奖", "我不能禁言主人！");
                        }
                    });
                    return;
                }
                // 忏悔
                else if (GeneralSettings.IsMute && e.detail == "忏悔" && MuteRecall.WillMute.ContainsKey(e.user_id))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e) || !Bot.BotIsAdmin(e.group_id)) return;
                        await Task.Delay(3800);
                        MuteRecall.WillMute.Remove(e.user_id);
                        await Post(SupportedAPI.set_group_ban, e.group_id, "忏悔", new SetGroupBanContent(e.group_id, e.user_id, 0));
                        await Post(e, "忏悔", "忏悔成功！！希望你保持纯真，保持野性的美。");
                    });
                    return;
                }

                // 随机反驳是
                if (e.detail == "是")
                {
                    if (e.user_id != GeneralSettings.Master && e.CheckThrow(40, out dice))
                    {
                        ColorfulCheckPass(sender, "反驳是", dice, 40);
                        _ = Post(e, "随机反驳是", "是你的头");
                    }
                    else if (e.user_id == GeneralSettings.Master)
                    {
                        _ = Post(e, "随机反驳是", "是你的头");
                    }
                }

                // 随机反驳不
                if (GeneralSettings.IsSayNo && SayNo.Trigger.Any(e.detail.Contains) && e.CheckThrow(GeneralSettings.PSayNo, out dice))
                {
                    GroupMessageContent content = new(e.group_id);
                    // 获取关键词在其中的位置
                    Dictionary<string, int> where = SayNo.Trigger
                        .ToDictionary(trigger => trigger, e.detail.IndexOf)
                        .Where(kvp => kvp.Value != -1)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    foreach (string keyword in where.Keys)
                    {
                        // 通常，只反驳第一个词，除非无可反驳才会找下一个词
                        int pos = where[keyword];
                        if (keyword == "不")
                        {
                            if (pos + 1 < e.detail.Length && !SayNo.IgnoreTriggerAfterNo.Any(e.detail[(pos + 1)..].Contains))
                            {
                                ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                                content.message.Add(new TextMessage(string.Format(SayNo.SayNoWords[new Random().Next(SayNo.SayNoWords.Count)], e.detail[pos + 1])));
                                break;
                            }
                        }
                        else if (keyword == "没")
                        {
                            if (pos + 1 < e.detail.Length)
                            {
                                ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                                if (e.detail[pos + 1] == '有')
                                {
                                    content.message.Add(new TextMessage(string.Format(SayNo.SayDontHaveWords[new Random().Next(SayNo.SayDontHaveWords.Count - 3)], e.detail[pos + 1])));
                                    break;
                                }
                                else
                                {
                                    content.message.Add(new TextMessage(string.Format(SayNo.SayDontHaveWords[new Random().Next(SayNo.SayDontHaveWords.Count)], e.detail[pos + 1])));
                                    break;
                                }
                            }
                        }
                        else if (keyword == "是")
                        {
                            if (pos + 1 < e.detail.Length && SayNo.TriggerAfterYes.Any(e.detail[(pos + 1)..].Contains))
                            {
                                ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                                content.message.Add(new TextMessage(SayNo.SayNotYesWords[new Random().Next(SayNo.SayNotYesWords.Count)]));
                                break;
                            }
                        }
                        else if (keyword == "别")
                        {
                            if (pos + 1 < e.detail.Length && !SayNo.IgnoreTriggerAfterNo.Any(e.detail[(pos + 1)..].Contains) && !SayNo.WillNotSayNo.Any(e.detail[(pos + 1)..].Contains))
                            {
                                ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                                content.message.Add(new TextMessage(string.Format(SayNo.SayDontWords[new Random().Next(SayNo.SayDontWords.Count)], e.detail[pos + 1])));
                                break;
                            }
                        }
                    }
                    if (content.message.Count > 0)
                    {
                        _ = Post(e, "随机反驳不", content);
                    }
                }
                else if (SayNo.TriggerBeforeNo.Any(e.detail.Contains) && GeneralSettings.IsSayNo && e.CheckThrow(GeneralSettings.PSayNo, out dice))
                {
                    GroupMessageContent content = new(e.group_id);
                    // 获取关键词在其中的位置
                    Dictionary<string, int> where = SayNo.TriggerBeforeNo
                        .ToDictionary(trigger => trigger, e.detail.IndexOf)
                        .Where(kvp => kvp.Value != -1)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    foreach (string keyword in where.Keys)
                    {
                        int pos = where[keyword];
                        string sayword = "";
                        if (keyword == "太")
                        {
                            if (pos + keyword.Length + 1 < e.detail.Length)
                            {
                                ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                                if (e.detail[(pos + keyword.Length + 1)..].Contains('了'))
                                {
                                    sayword = e.detail[pos..].Replace(keyword, "");
                                    sayword = sayword.Replace("了", "");
                                }
                                else
                                {
                                    sayword = e.detail.Replace(keyword, "");
                                }
                                if (sayword.Length > 2) sayword = sayword[..2];
                                content.message.Add(new TextMessage(SayNo.SaySpecialNoWords[new Random().Next(SayNo.SaySpecialNoWords.Count)] + sayword));
                                break;
                            }
                        }
                        else
                        {
                            if (pos + keyword.Length + 1 < e.detail.Length)
                            {
                                ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                                sayword = e.detail[pos..].Replace(keyword, "");
                                if (sayword.Length > 2) sayword = sayword[..2];
                                List<string> NewSayWords = [];
                                NewSayWords.Add("太");
                                NewSayWords.AddRange(SayNo.SaySpecialNoWords);
                                NewSayWords.Remove(keyword);
                                sayword = NewSayWords[new Random().Next(NewSayWords.Count)] + sayword;
                                if (sayword[0] == '太') sayword += "了";
                                content.message.Add(new TextMessage(sayword));
                            }
                        }
                    }
                    if (content.message.Count > 0)
                    {
                        _ = Post(e, "随机反驳不", content);
                    }
                }
                else if (e.detail.Contains("可以") && !e.detail.Contains('不') && e.CheckThrow(GeneralSettings.PSayNo, out dice))
                {
                    ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                    if (dice < (GeneralSettings.PSayNo / 2))
                    {
                        _ = Post(e, "随机反驳不", "可以");
                    }
                    else
                    {
                        _ = Post(e, "随机反驳不", "不可以");
                    }
                }
                else if (e.detail.Contains('能') && !e.detail.Contains('不') && !e.detail.Contains('技') && !e.detail.Contains('可') && e.CheckThrow(GeneralSettings.PSayNo, out dice))
                {
                    ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                    if (dice < (GeneralSettings.PSayNo / 2))
                    {
                        _ = Post(e, "随机反驳不", "能");
                    }
                    else
                    {
                        _ = Post(e, "随机反驳不", "不能");
                    }
                }
                else if (e.detail.Contains("可能") && !e.detail.Contains('不') && e.CheckThrow(GeneralSettings.PSayNo, out dice))
                {
                    ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                    if (dice < (GeneralSettings.PSayNo / 2))
                    {
                        _ = Post(e, "随机反驳不", "可能");
                    }
                    else
                    {
                        _ = Post(e, "随机反驳不", "不可能");
                    }
                }

                // 反向艾特
                IEnumerable<AtMessage> temp_at = e.message.Where(m => m.type == "at").Cast<AtMessage>().Where(m => m.data.qq == $"{GeneralSettings.BotQQ}");
                if (temp_at.Any())
                {
                    if (GeneralSettings.IsReverseAt && e.CheckThrow(GeneralSettings.PReverseAt, out dice))
                    {
                        ColorfulCheckPass(sender, "反向艾特", dice, GeneralSettings.PReverseAt);
                        foreach (AtMessage at in temp_at)
                        {
                            at.data.qq = e.user_id.ToString();
                            GroupMessageContent content = new(e.group_id);
                            content.message.AddRange(e.message);
                            _ = Post(e, "反向艾特", content);
                        }
                    }
                    return;
                }

                // 随机OSM
                if (GeneralSettings.IsOSM && !Ignore.RepeatIgnore.Contains(e.detail) && e.CheckThrow(GeneralSettings.POSM, out dice))
                {
                    ColorfulCheckPass(sender, "随机OSM", dice, GeneralSettings.POSM);
                    GroupMessageContent content = new(e.group_id);
                    string img = new Random().Next(3) switch
                    {
                        0 => "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\newosm.jpg",
                        1 => "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\osm.gif",
                        _ => "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\osm.jpg",
                    };
                    content.message.Add(new ImageMessage(img));
                    _ = Post(e, "Image", content);
                    return;
                }

                // 随机复读
                if (GeneralSettings.IsRepeat && !Ignore.RepeatIgnore.Contains(e.detail) && e.CheckThrow(GeneralSettings.PRepeat, out dice))
                {
                    int delay = GeneralSettings.RepeatDelay[0] + new Random().Next(GeneralSettings.RepeatDelay[1] - GeneralSettings.RepeatDelay[0]);
                    ColorfulCheckPass(sender, "随机复读", dice, GeneralSettings.PRepeat, delay);
                    GroupMessageContent content = new(e.group_id);
                    content.message.AddRange(e.message);
                    _ = Post(e, "随机复读", content, delay * 1000);
                    return;
                }

                // 随机叫哥
                if (GeneralSettings.IsCallBrother && e.CheckThrow(GeneralSettings.PCallBrother, out dice))
                {
                    int delay = GeneralSettings.RepeatDelay[0] + new Random().Next(GeneralSettings.RepeatDelay[1]);
                    ColorfulCheckPass(sender, "随机叫哥", dice, GeneralSettings.PCallBrother, delay);
                    string name = (sender.card != "" ? sender.card : sender.nickname).Trim();
                    int pos = new Random().Next(name.Length - 1);
                    GroupMessageContent content = new(e.group_id);
                    content.message.Add(new AtMessage(e.user_id));
                    content.message.Add(new TextMessage(string.Concat(name.AsSpan(pos, 2), "哥")));
                    _ = Post(e, "随机叫哥", content, delay * 1000);
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        public static async Task<bool> CheckBlackList(GroupMessageEvent e)
        {
            // 黑名单
            if (e.user_id == GeneralSettings.Master) return true;
            if (!BlackList.Times.ContainsKey(e.user_id))
            {
                BlackList.Times.Add(e.user_id, 1);
                return true;
            }
            else if (BlackList.Times.TryGetValue(e.user_id, out long bltimes) && bltimes > 5)
            {
                return false;
            }
            else if (++bltimes == 5)
            {
                BlackList.Times[e.user_id] = 6;
                GroupMessageContent content = new(e.group_id);
                content.message.Add(new AtMessage(e.user_id));
                content.message.Add(new TextMessage("警告：你已因短时间内频繁操作被禁止使用BOT指令" + (GeneralSettings.BlackFrozenTime / 60) + "分钟" + (GeneralSettings.BlackFrozenTime % 60) + "秒。"));
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000 * GeneralSettings.BlackFrozenTime);
                    BlackList.Times.Remove(e.user_id);
                });
                await Post(e, "黑名单", content);
                return false;
            }
            else
            {
                BlackList.Times[e.user_id] = bltimes;
                return true;
            }
        }

        public static async Task Mute(GroupMessageEvent e)
        {
            if (!Bot.BotIsAdmin(e.group_id)) return;
            bool unmute = e.detail.Contains("解禁");
            string[] strs = Regex.Split(e.detail, @"\s+");
            if (!unmute && strs.Length < 2) return;
            if (e.user_id == GeneralSettings.Master || (unmute && GeneralSettings.UnMuteAccessGroup.Contains(e.user_id)) || (!unmute && GeneralSettings.MuteAccessGroup.Contains(e.user_id)))
            {
                strs = Regex.Split(e.detail.Replace(".osm mute", "").Replace("禁言", "").Replace("解禁", "").Replace("所有人", "").Trim(), @"\s+");
                long time = 0;
                if ((!unmute && strs.Length > 1 && long.TryParse(strs[^1], out time) && time >= 0 && time < 2592000) || unmute)
                {
                    List<long> qqlist = [];
                    List<IContent> list = [];
                    foreach (string str in unmute ? strs : strs[..^1])
                    {
                        if (long.TryParse(str.Replace(@"@", "").Trim(), out long qq))
                        {
                            SetGroupBanContent content = new(e.group_id, qq, time);
                            list.Add(content);
                            qqlist.Add(qq);
                        }
                    }
                    await Post(SupportedAPI.set_group_ban, e.group_id, "禁言指令", list);
                    if (time > 0)
                    {
                        await Task.Delay(3000);
                        foreach (long qq in qqlist)
                        {
                            if (MuteRecall.Muted[e.group_id].ContainsKey(qq)) MuteRecall.Muted[e.group_id][qq] = GeneralSettings.Master;
                            else MuteRecall.Muted[e.group_id].Add(qq, GeneralSettings.Master);
                        }
                    }
                    return;
                }
                await Post(e, "OSM指令", MasterCommand.Execute(".osm mute", "", strs.Length > 1 ? strs[1..] : []));
            }
            else await Post(e, "OSM指令", "你没有权限使用此指令。");
        }

        public static async Task Mute(GroupMessageEvent e, IEnumerable<long> qqlist)
        {
            if (!Bot.BotIsAdmin(e.group_id)) return;
            bool unmute = e.detail.Contains("解禁");
            if (e.user_id == GeneralSettings.Master || (unmute && GeneralSettings.UnMuteAccessGroup.Contains(e.user_id)) || (!unmute && GeneralSettings.MuteAccessGroup.Contains(e.user_id)))
            {
                string[] strs = Regex.Split(e.detail.Replace("禁言", "").Replace("解禁", "").Replace("所有人", "").Trim(), @"\s+");
                long mute_time = unmute ? 0 : GeneralSettings.MuteTime[0] + new Random().NextInt64(GeneralSettings.MuteTime[1] - GeneralSettings.MuteTime[0]);
                if (long.TryParse(strs[^1], out long time) && time >= 0 && time < 2592000)
                {
                    mute_time = time;
                }
                List<IContent> list = [];
                foreach (long qq in qqlist)
                {
                    SetGroupBanContent content = new(e.group_id, qq, mute_time);
                    list.Add(content);
                }
                await Post(SupportedAPI.set_group_ban, e.group_id, "批量禁言指令", list);
                if (mute_time > 0)
                {
                    await Task.Delay(3000);
                    foreach (long qq in qqlist)
                    {
                        if (MuteRecall.Muted[e.group_id].ContainsKey(qq)) MuteRecall.Muted[e.group_id][qq] = GeneralSettings.Master;
                        else MuteRecall.Muted[e.group_id].Add(qq, GeneralSettings.Master);
                    }
                }
            }
            else await Post(e, "OSM指令", "你没有权限使用此指令。");
        }

        public static async Task MuteGroup(GroupMessageEvent e)
        {
            if (!Bot.BotIsAdmin(e.group_id)) return;
            if (e.user_id == GeneralSettings.Master || GeneralSettings.MuteAccessGroup.Contains(e.user_id))
            {
                string str = e.detail.Replace(".osm mutegroup", "").Replace("跨群禁言", "").Trim();
                string[] strs = Regex.Split(str, @"\s+");
                if (strs.Length > 2)
                {
                    string str_group = strs[0].Replace(@"@", "").Trim();
                    string str_qq = strs[1].Replace(@"@", "").Trim();
                    if (long.TryParse(str_group, out long group) && long.TryParse(str_qq, out long qq) && long.TryParse(strs[2], out long time) && time >= 0 && time < 2592000)
                    {
                        SetGroupBanContent content = new(group, qq, time);
                        await Post(SupportedAPI.set_group_ban, group, "OSM指令", content);
                        if (time > 0)
                        {
                            await Task.Delay(3000);
                            if (MuteRecall.Muted[e.group_id].ContainsKey(qq)) MuteRecall.Muted[e.group_id][qq] = GeneralSettings.Master;
                            else MuteRecall.Muted[e.group_id].Add(qq, GeneralSettings.Master);
                        }
                    }
                    else await Post(e, "OSM指令", MasterCommand.Execute(".osm mute", "", strs.Length > 1 ? strs[1..] : []));
                }
                else await Post(e, "OSM指令", MasterCommand.Execute(".osm mute", "", strs.Length > 1 ? strs[1..] : []));
            }
            else await Post(e, "OSM指令", "你没有权限使用此指令。");
        }

        public static void ColorfulCheckPass(Sender sender, string function, long dice, long probability, int delay = 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            if (delay > 0)
            {
                Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} ");
            }
            Console.Write($"{sender.user_id}（{(sender.card != "" ? sender.card : sender.nickname)}）的{function}检定通过：{dice} < {probability}");
            if (delay > 0)
            {
                Console.Write(" -> " + delay + "秒后执行");
            }
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static async Task Post(GroupMessageEvent e, string function, string text, int delay = 0)
        {
            string result = (await e.SendMessage(text, delay)).ReasonPhrase ?? "";
            Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} F/");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(function);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($" G/{e.group_id} <- {text} {result}");
        }

        public static async Task Post(GroupMessageEvent e, string function, GroupMessageContent content, int delay = 0)
        {
            string result = (await e.SendMessage(content, delay)).ReasonPhrase ?? "";
            Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} F/");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(function);
            Console.ForegroundColor = ConsoleColor.Gray;
            if (!GeneralSettings.IsDebug)
            {
                Console.WriteLine($" G/{e.group_id} <- {content.detail} {result}");
            }
            else
            {
                Console.WriteLine($" G/{e.group_id} <- {JsonTools.GetString(content)} {result}");
            }
        }

        public static async Task Post(string api, long group_id, string function, IContent content)
        {
            string result = (await HTTPPost.Post(api, content)).ReasonPhrase ?? "";
            Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} F/");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(function);
            Console.ForegroundColor = ConsoleColor.Gray;
            if (!GeneralSettings.IsDebug)
            {
                Console.WriteLine($" G/{group_id} <- {content.detail} {result}");
            }
            else
            {
                Console.WriteLine($" G/{group_id} <- {HTTPHelper.GetJsonString(api, content)} {result}");
            }
        }

        public static async Task Post(string api, long group_id, string function, IEnumerable<IContent> contents)
        {
            await HTTPPost.Post(api, contents);
            Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} F/");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(function);
            Console.ForegroundColor = ConsoleColor.Gray;
            if (!GeneralSettings.IsDebug)
            {
                Console.WriteLine($" G/{group_id} <- 已在后台执行");
            }
            else
            {
                Console.WriteLine($" G/{group_id} <- 已在后台执行");
            }
        }
    }
}
﻿using Milimoe.OneBot.Framework;
using Milimoe.OneBot.Framework.Utility;
using Milimoe.OneBot.Model.Content;
using Milimoe.OneBot.Model.Event;
using Milimoe.OneBot.Model.Message;
using Milimoe.OneBot.Model.Other;
using Milimoe.OneBot.Model.QuickReply;
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
                    MasterCommand.Execute(e.detail, e.user_id, onOSMCore, e.group_id, true);
                    return;
                }

                if (e.detail.Length >= 5 && (e.detail[..5] == "禁言所有人" || e.detail[..5] == "解禁所有人") && (e.user_id == GeneralSettings.Master || GeneralSettings.UnMuteAccessGroup.Union(GeneralSettings.MuteAccessGroup).Contains(e.user_id)) && Bot.GroupMembers.TryGetValue(e.group_id, out List<Member>? members) && members != null)
                {
                    TaskUtility.NewTask(async () => await Bot.Mute(e.user_id, e.group_id, e.detail, members.Where(m => m.user_id != GeneralSettings.Master).Select(m => m.user_id)));
                    return;
                }

                if (e.detail != "禁言抽奖" && e.detail.Length >= 2 && MuteCommands.Any(e.detail[..2].Contains))
                {
                    TaskUtility.NewTask(async () => await Bot.Mute(e.user_id, e.group_id, e.detail));
                    return;
                }

                if (e.detail.Length >= 4 && e.detail[..4] == "跨群禁言")
                {
                    TaskUtility.NewTask(async () => await Bot.MuteGroup(e.user_id, e.group_id, e.detail));
                    return;
                }

                // 撤回消息
                if ((e.user_id == GeneralSettings.Master || GeneralSettings.RecallAccessGroup.Contains(e.user_id)) && e.detail.Contains("撤回；") && e.message.Any(m => m.type == "reply"))
                {
                    ReplyMessage reply = (ReplyMessage)e.message.Where(m => m.type == "reply").First();
                    if (int.TryParse(reply.data.id, out int id))
                    {
                        TaskUtility.NewTask(async () => await Bot.SendMessage(SupportedAPI.delete_msg, e.group_id, "撤回", new DeleteMsgContent(id), true));
                        TaskUtility.NewTask(async () => await Bot.SendMessage(SupportedAPI.delete_msg, e.group_id, "撤回", new DeleteMsgContent(e.real_id), true));
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
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("https://iw233.cn/api.php?sort=random"));
                        await Bot.SendGroupMessage(e.group_id, "Image", content);
                        return;
                    });
                }
                if (e.detail.Contains("白毛"))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("https://iw233.cn/api.php?sort=yin"));
                        await Bot.SendGroupMessage(e.group_id, "Image", content);
                        return;
                    });
                }
                if (e.detail == "猫耳")
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("https://iw233.cn/api.php?sort=cat"));
                        await Bot.SendGroupMessage(e.group_id, "Image", content);
                        return;
                    });
                }
                if (e.detail == "壁纸")
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("https://iw233.cn/api.php?sort=pc"));
                        await Bot.SendGroupMessage(e.group_id, "Image", content);
                        return;
                    });
                }
                if (e.detail == "新闻")
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("https://api.03c3.cn/api/zb"));
                        await Bot.SendGroupMessage(e.group_id, "Image", content);
                        return;
                    });
                }
                if (e.detail.Contains("来龙"))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\long\long (" + (new Random().Next(1540) + 1) + ").jpg"));
                        await Bot.SendGroupMessage(e.group_id, "Image", content);
                        return;
                    });
                }
                if (e.detail.Contains("丁真") || e.detail == "一眼丁真" || e.detail == "一眼顶针")
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\dingzhen\dz" + (new Random().Next(82) + 1) + ".jpg"));
                        await Bot.SendGroupMessage(e.group_id, "Image", content);
                        return;
                    });
                }
                if (EEWords.Any(e.detail.Contains))
                {
                    if (BlackList.Times.TryGetValue(e.user_id, out long bltimes) && bltimes > 5) return;
                    GroupMessageContent content = new(e.group_id);
                    content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\ee.png"));
                    _ = Bot.SendGroupMessage(e.group_id, "Image", content);
                    return;
                }

                // 发音频API
                if (e.detail.Contains("kun", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["ikun"]));
                        await Bot.SendGroupMessage(e.group_id, "Record", content);
                    });
                    return;
                }
                if (e.detail.Contains("csgo", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["懂CSGO"]));
                        await Bot.SendGroupMessage(e.group_id, "Record", content);
                    });
                    return;
                }
                if (e.detail.Contains("架不住") || e.detail.Contains("打不死") || e.detail.Contains("不玩了"))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["令人沮丧的游戏"]));
                        await Bot.SendGroupMessage(e.group_id, "Record", content);
                    });
                }
                if (e.detail.Contains("man", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["man"]));
                        await Bot.SendGroupMessage(e.group_id, "Record", content);
                    });
                    return;
                }
                if (e.detail.Contains("马云", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["马云"]));
                        await Bot.SendGroupMessage(e.group_id, "Record", content);
                    });
                    return;
                }
                if (e.detail.Contains("电锯", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["电锯"]));
                        await Bot.SendGroupMessage(e.group_id, "Record", content);
                    });
                    return;
                }
                if (e.detail.Contains("疤王", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["疤王"]));
                        await Bot.SendGroupMessage(e.group_id, "Record", content);
                    });
                    return;
                }
                if (e.detail.Contains("终极", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["终极"]));
                        await Bot.SendGroupMessage(e.group_id, "Record", content);
                    });
                    return;
                }
                if (e.detail.Contains("高考", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList["高考"]));
                        await Bot.SendGroupMessage(e.group_id, "Record", content);
                    });
                    return;
                }
                if (e.detail.Contains("音乐", StringComparison.CurrentCultureIgnoreCase))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new RecordMessage(Music.MusicList[Music.MusicList.Keys.ToArray()[new Random().Next(Music.MusicList.Count)]]));
                        await Bot.SendGroupMessage(e.group_id, "Record", content);
                    });
                    return;
                }

                // 我的运势
                if (e.detail == "我的运势")
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new AtMessage(e.user_id));
                        if (Daily.UserDailys.TryGetValue(e.user_id, out string? value) && value != null && value.Trim() != "")
                        {
                            content.message.Add(new TextMessage("你已看过你的今日运势：\r\n"));
                            content.message.Add(new TextMessage(value));
                            await Bot.SendGroupMessage(e.group_id, "我的运势", content);
                        }
                        else
                        {
                            int seq = new Random().Next(Daily.DailyContent.Count);
                            string text = Daily.DailyContent[seq];
                            Daily.UserDailys.Add(e.user_id, text);
                            content.message.Add(new TextMessage("你的今日运势是：\r\n" + text));
                            await Bot.SendGroupMessage(e.group_id, "我的运势", content);
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
                            await Bot.SendGroupMessage(e.group_id, "我的运势配图", content);
                        }
                    });
                    return;
                }
                if (e.detail == "重置运势" && Daily.UserDailys.ContainsKey(e.user_id))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
                        Daily.UserDailys.Remove(e.user_id);
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new AtMessage(e.user_id));
                        content.message.Add(new TextMessage("你的今日运势已重置。"));
                        await Bot.SendGroupMessage(e.group_id, "重置运势", content);
                    });
                    return;
                }
                if (e.detail.Length > 4 && e.detail[..2] == "查看" && (e.detail[^2..] == "运势"))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return;
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
                                    content.message.Add(new TextMessage(Bot.GetMemberNickName(e.group_id, qq) + "（" + qq + "）的今日运势是：\r\n" + daily));
                                    await Bot.SendGroupMessage(e.group_id, "查看运势", content);
                                }
                                else
                                {
                                    await Bot.SendGroupMessage(e.group_id, "查看运势", "TA今天还没有抽取运势哦，快去提醒TA！");
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
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id) || !Bot.BotIsAdmin(e.group_id)) return;
                        if (e.user_id != GeneralSettings.Master)
                        {
                            await Bot.SendGroupMessage(e.group_id, "禁言抽奖", "2秒后开奖～\r\n如需要忏悔，请在开奖后3秒内发送忏悔，开奖前发送无效。");
                            await Task.Delay(2000);
                            if (!MuteRecall.WillMute.ContainsKey(e.user_id)) MuteRecall.WillMute.Add(e.user_id, e.user_id);
                            long mute_time = GeneralSettings.MuteTime[0] + new Random().NextInt64(GeneralSettings.MuteTime[1] - GeneralSettings.MuteTime[0]);
                            await Bot.SendGroupMessage(e.group_id, "禁言抽奖", "开奖啦！禁言时长：" + (mute_time / 60) + "分钟" + (mute_time % 60) + "秒。\r\n" + "你现在有3秒时间发送忏悔拒绝领奖！");
                            await Task.Delay(3200);
                            await Bot.SendMessage(SupportedAPI.set_group_ban, e.group_id, "禁言抽奖", new SetGroupBanContent(e.group_id, e.user_id, mute_time), true);
                            MuteRecall.WillMute.Remove(e.user_id);
                        }
                        else
                        {
                            _ = Bot.SendGroupMessage(e.group_id, "禁言抽奖", "我不能禁言主人！");
                        }
                    });
                    return;
                }
                // 忏悔
                else if (GeneralSettings.IsMute && e.detail == "忏悔" && MuteRecall.WillMute.ContainsKey(e.user_id))
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await Bot.CheckBlackList(true, e.user_id, e.group_id) || !Bot.BotIsAdmin(e.group_id)) return;
                        await Task.Delay(3800);
                        MuteRecall.WillMute.Remove(e.user_id);
                        await Bot.SendMessage(SupportedAPI.set_group_ban, e.group_id, "忏悔", new SetGroupBanContent(e.group_id, e.user_id, 0), true);
                        await Bot.SendGroupMessage(e.group_id, "忏悔", "忏悔成功！！希望你保持纯真，保持野性的美。");
                    });
                    return;
                }

                // 随机反驳是
                if (e.detail == "是")
                {
                    if (e.user_id != GeneralSettings.Master && e.CheckThrow(40, out dice))
                    {
                        Bot.ColorfulCheckPass(sender, "反驳是", dice, 40);
                        _ = Bot.SendGroupMessage(e.group_id, "随机反驳是", "是你的头");
                    }
                    else if (e.user_id == GeneralSettings.Master)
                    {
                        _ = Bot.SendGroupMessage(e.group_id, "随机反驳是", "是你的头");
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
                                Bot.ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                                content.message.Add(new TextMessage(string.Format(SayNo.SayNoWords[new Random().Next(SayNo.SayNoWords.Count)], e.detail[pos + 1])));
                                break;
                            }
                        }
                        else if (keyword == "没")
                        {
                            if (pos + 1 < e.detail.Length)
                            {
                                Bot.ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
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
                                Bot.ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                                content.message.Add(new TextMessage(SayNo.SayNotYesWords[new Random().Next(SayNo.SayNotYesWords.Count)]));
                                break;
                            }
                        }
                        else if (keyword == "别")
                        {
                            if (pos + 1 < e.detail.Length && !SayNo.IgnoreTriggerAfterNo.Any(e.detail[(pos + 1)..].Contains) && !SayNo.WillNotSayNo.Any(e.detail[(pos + 1)..].Contains))
                            {
                                Bot.ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                                content.message.Add(new TextMessage(string.Format(SayNo.SayDontWords[new Random().Next(SayNo.SayDontWords.Count)], e.detail[pos + 1])));
                                break;
                            }
                        }
                    }
                    if (content.message.Count > 0)
                    {
                        _ = Bot.SendGroupMessage(e.group_id, "随机反驳不", content);
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
                                Bot.ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
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
                                Bot.ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
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
                        _ = Bot.SendGroupMessage(e.group_id, "随机反驳不", content);
                    }
                }
                else if (e.detail.Contains("可以") && !e.detail.Contains('不') && e.CheckThrow(GeneralSettings.PSayNo, out dice))
                {
                    Bot.ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                    if (dice < (GeneralSettings.PSayNo / 2))
                    {
                        _ = Bot.SendGroupMessage(e.group_id, "随机反驳不", "可以");
                    }
                    else
                    {
                        _ = Bot.SendGroupMessage(e.group_id, "随机反驳不", "不可以");
                    }
                }
                else if (e.detail.Contains('能') && !e.detail.Contains('不') && !e.detail.Contains('技') && !e.detail.Contains('可') && e.CheckThrow(GeneralSettings.PSayNo, out dice))
                {
                    Bot.ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                    if (dice < (GeneralSettings.PSayNo / 2))
                    {
                        _ = Bot.SendGroupMessage(e.group_id, "随机反驳不", "能");
                    }
                    else
                    {
                        _ = Bot.SendGroupMessage(e.group_id, "随机反驳不", "不能");
                    }
                }
                else if (e.detail.Contains("可能") && !e.detail.Contains('不') && e.CheckThrow(GeneralSettings.PSayNo, out dice))
                {
                    Bot.ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                    if (dice < (GeneralSettings.PSayNo / 2))
                    {
                        _ = Bot.SendGroupMessage(e.group_id, "随机反驳不", "可能");
                    }
                    else
                    {
                        _ = Bot.SendGroupMessage(e.group_id, "随机反驳不", "不可能");
                    }
                }

                // 反向艾特
                IEnumerable<AtMessage> temp_at = e.message.Where(m => m.type == "at").Cast<AtMessage>().Where(m => m.data.qq == $"{GeneralSettings.BotQQ}");
                if (temp_at.Any())
                {
                    if (GeneralSettings.IsReverseAt && e.CheckThrow(GeneralSettings.PReverseAt, out dice))
                    {
                        Bot.ColorfulCheckPass(sender, "反向艾特", dice, GeneralSettings.PReverseAt);
                        foreach (AtMessage at in temp_at)
                        {
                            at.data.qq = e.user_id.ToString();
                            GroupMessageContent content = new(e.group_id);
                            content.message.AddRange(e.message);
                            _ = Bot.SendGroupMessage(e.group_id, "反向艾特", content);
                        }
                    }
                    return;
                }

                // 随机OSM
                if (GeneralSettings.IsOSM && !Ignore.RepeatIgnore.Contains(e.detail) && e.CheckThrow(GeneralSettings.POSM, out dice))
                {
                    Bot.ColorfulCheckPass(sender, "随机OSM", dice, GeneralSettings.POSM);
                    GroupMessageContent content = new(e.group_id);
                    string img = new Random().Next(3) switch
                    {
                        0 => "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\newosm.jpg",
                        1 => "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\osm.gif",
                        _ => "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\osm.jpg",
                    };
                    content.message.Add(new ImageMessage(img));
                    _ = Bot.SendGroupMessage(e.group_id, "Image", content);
                    return;
                }

                // 随机复读
                if (GeneralSettings.IsRepeat && !Ignore.RepeatIgnore.Contains(e.detail) && e.CheckThrow(GeneralSettings.PRepeat, out dice))
                {
                    int delay = GeneralSettings.RepeatDelay[0] + new Random().Next(GeneralSettings.RepeatDelay[1] - GeneralSettings.RepeatDelay[0]);
                    Bot.ColorfulCheckPass(sender, "随机复读", dice, GeneralSettings.PRepeat, delay);
                    GroupMessageContent content = new(e.group_id);
                    content.message.AddRange(e.message);
                    _ = Bot.SendGroupMessage(e.group_id, "随机复读", content, delay * 1000);
                    return;
                }

                // 随机叫哥
                if (GeneralSettings.IsCallBrother && e.CheckThrow(GeneralSettings.PCallBrother, out dice))
                {
                    int delay = GeneralSettings.RepeatDelay[0] + new Random().Next(GeneralSettings.RepeatDelay[1]);
                    Bot.ColorfulCheckPass(sender, "随机叫哥", dice, GeneralSettings.PCallBrother, delay);
                    string name = (sender.card != "" ? sender.card : sender.nickname).Trim();
                    int pos = new Random().Next(name.Length - 1);
                    GroupMessageContent content = new(e.group_id);
                    content.message.Add(new AtMessage(e.user_id));
                    content.message.Add(new TextMessage(string.Concat(name.AsSpan(pos, 2), "哥")));
                    _ = Bot.SendGroupMessage(e.group_id, "随机叫哥", content, delay * 1000);
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
    }
}
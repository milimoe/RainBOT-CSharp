﻿using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using Milimoe.OneBot.Framework;
using Milimoe.OneBot.Model.Content;
using Milimoe.OneBot.Model.Event;
using Milimoe.OneBot.Model.Message;
using Milimoe.OneBot.Model.Other;
using Milimoe.OneBot.Model.QuickReply;
using Milimoe.RainBOT.Command;
using Milimoe.RainBOT.Model;
using Milimoe.RainBOT.Settings;

namespace Milimoe.RainBOT.ListeningTask
{
    public class GroupMessageTask
    {
        private static long dice = 0;
        private readonly static string[] EEWords = ["ee", "鹅鹅", "呃呃", "谔谔", "饿饿"];
        private readonly static string[] MuteCommands = ["禁言", "解禁"];

        public static async Task<GroupMsgEventQuickReply?> ListeningTask_handler(GroupMessageEvent e)
        {
            GroupMsgEventQuickReply? quick_reply = null;

            try
            {
                Sender sender = e.sender;
                if (e.user_id == 0 || e.sender.user_id == 0) return quick_reply;

                if (GeneralSettings.DebugGroupID != 0 && e.group_id != GeneralSettings.DebugGroupID)
                {
                    Console.WriteLine($"{e.group_id} 不是沙盒群聊，已经过滤。");
                    return quick_reply;
                }
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
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    MasterCommand.Execute(e.detail, e.user_id, onOSMCore, e.group_id, true);
                    return quick_reply;
                }

                if (e.detail.Length >= 5 && (e.detail[..5] == "禁言所有人" || e.detail[..5] == "解禁所有人") && (e.user_id == GeneralSettings.Master || GeneralSettings.UnMuteAccessGroup.Union(GeneralSettings.MuteAccessGroup).Contains(e.user_id)) && Bot.GroupMembers.TryGetValue(e.group_id, out List<Member>? members) && members != null)
                {
                    await Bot.Mute(e.user_id, e.group_id, e.detail, members.Where(m => m.user_id != GeneralSettings.Master).Select(m => m.user_id));
                    return quick_reply;
                }

                if (e.detail != "禁言抽奖" && e.detail.Length >= 2 && MuteCommands.Any(e.detail[..2].Contains))
                {
                    await Bot.Mute(e.user_id, e.group_id, e.detail);
                    return quick_reply;
                }

                if (e.detail.Length >= 4 && e.detail[..4] == "跨群禁言")
                {
                    await Bot.MuteGroup(e.user_id, e.group_id, e.detail);
                    return quick_reply;
                }

                // 撤回消息
                if ((e.user_id == GeneralSettings.Master || GeneralSettings.RecallAccessGroup.Contains(e.user_id)) && e.detail.Contains("撤回；") && e.message.Any(m => m.type == "reply"))
                {
                    ReplyMessage reply = (ReplyMessage)e.message.Where(m => m.type == "reply").First();
                    if (int.TryParse(reply.data.id, out int id))
                    {
                        await Bot.SendMessage(SupportedAPI.delete_msg, e.group_id, "撤回", new DeleteMsgContent(id), true);
                        await Bot.SendMessage(SupportedAPI.delete_msg, e.group_id, "撤回", new DeleteMsgContent(e.real_id), true);
                        return quick_reply;
                    }
                }

                // 精华消息
                if ((e.user_id == GeneralSettings.Master || Bot.IsAdmin(e.group_id, e.user_id)) && e.detail.Contains("精华；") && e.message.Any(m => m.type == "reply"))
                {
                    ReplyMessage reply = (ReplyMessage)e.message.Where(m => m.type == "reply").First();
                    if (int.TryParse(reply.data.id, out int id))
                    {
                        if (e.detail.Contains("取消精华；"))
                        {
                            await Bot.SendMessage(SupportedAPI.delete_essence_msg, e.group_id, "取消精华", new DeleteEssenceMsgContent(id), true);
                            await Bot.SendMessage(SupportedAPI.delete_msg, e.group_id, "撤回", new DeleteMsgContent(e.real_id), true);
                            return quick_reply;
                        }
                        else
                        {
                            await Bot.SendMessage(SupportedAPI.set_essence_msg, e.group_id, "设置精华", new EssenceMsgContent(id), true);
                            await Bot.SendMessage(SupportedAPI.delete_msg, e.group_id, "撤回", new DeleteMsgContent(e.real_id), true);
                            return quick_reply;
                        }
                    }
                }

                if (Ignore.CustomIgnore.Any(e.detail.Contains) && Bot.BotIsAdmin(e.group_id))
                {
                    await Bot.SendMessage(SupportedAPI.delete_msg, e.group_id, "撤回", new DeleteMsgContent(e.real_id), true);
                    await Bot.SendMessage(SupportedAPI.set_group_ban, e.group_id, "禁言", new SetGroupBanContent(e.group_id, e.user_id, 120), true);
                    return quick_reply;
                }

                if (e.detail == "查询服务器启动时间")
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/test/getlastlogintime") ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "查询服务器启动时间", msg);
                    }
                    return quick_reply;
                }
                
                if (e.detail.StartsWith("查询任务计划"))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    string detail = e.detail.Replace("查询任务计划", "").Trim();
                    string msg = await Bot.HttpGet<string>($"https://api.milimoe.com/test/gettask?name={detail}") ?? "";
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "查询任务计划", msg);
                    }
                    return quick_reply;
                }

                if (!GeneralSettings.IsRun)
                {
                    return quick_reply;
                }

                if (await FunGame.Handler(e))
                {
                    return quick_reply;
                }
                if (GeneralSettings.FunGameGroup.Contains(e.group_id))
                {
                }

                // 发图API
                if (e.detail == "来图")
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    Guid guid = await Bot.DownloadImageStream("https://iw233.cn/api.php?sort=random", "https://weibo.com/");
                    if (guid != Guid.Empty)
                    {
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\download\" + guid.ToString() + ".jpg"));
                        await Bot.SendGroupMessage(e.group_id, "Image", content);
                    }
                    return quick_reply;
                }
                if (e.detail.Contains("白毛"))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    Guid guid = await Bot.DownloadImageStream("https://iw233.cn/api.php?sort=yin", "https://weibo.com/");
                    if (guid != Guid.Empty)
                    {
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\download\" + guid.ToString() + ".jpg"));
                        await Bot.SendGroupMessage(e.group_id, "Image", content);
                    }
                    return quick_reply;
                }
                if (e.detail == "猫耳")
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    Guid guid = await Bot.DownloadImageStream("https://iw233.cn/api.php?sort=cat", "https://weibo.com/");
                    if (guid != Guid.Empty)
                    {
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\download\" + guid.ToString() + ".jpg"));
                        await Bot.SendGroupMessage(e.group_id, "Image", content);
                    }
                    return quick_reply;
                }
                if (e.detail == "壁纸")
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    Guid guid = await Bot.DownloadImageStream("https://iw233.cn/api.php?sort=pc", "https://weibo.com/");
                    if (guid != Guid.Empty)
                    {
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\download\" + guid.ToString() + ".jpg"));
                        await Bot.SendGroupMessage(e.group_id, "Image", content);
                    }
                    return quick_reply;
                }
                if (e.detail == "新闻")
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    GroupMessageContent content = new(e.group_id);
                    content.message.Add(new ImageMessage("https://api.03c3.cn/api/zb"));
                    await Bot.SendGroupMessage(e.group_id, "Image", content);
                    return quick_reply;
                }
                if (e.detail == "买家秀")
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    GroupMessageContent content = new(e.group_id);
                    content.message.Add(new ImageMessage("https://api.03c3.cn/api/taobaoBuyerShow"));
                    await Bot.SendGroupMessage(e.group_id, "Image", content);
                    return quick_reply;
                }
                if (e.detail.Contains("来龙"))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    GroupMessageContent content = new(e.group_id);
                    content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\long\long (" + (new Random().Next(1540) + 1) + ").jpg"));
                    await Bot.SendGroupMessage(e.group_id, "Image", content);
                    return quick_reply;
                }
                if (e.detail == "一眼丁真" || e.detail == "一眼顶针")
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    GroupMessageContent content = new(e.group_id);
                    content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\dingzhen\dz" + (new Random().Next(82) + 1) + ".jpg"));
                    await Bot.SendGroupMessage(e.group_id, "Image", content);
                    return quick_reply;
                }
                if (EEWords.Any(e.detail.Contains) && e.CheckThrow(20, out _))
                {
                    GroupMessageContent content = new(e.group_id);
                    content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\ee.png"));
                    await Bot.SendGroupMessage(e.group_id, "Image", content);
                    return quick_reply;
                }

                // 发音频API
                var match_music = Music.MusicList.Keys.Where(s => e.detail.Contains(s, StringComparison.CurrentCultureIgnoreCase));
                if (match_music.Any())
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    GroupMessageContent content = new(e.group_id);
                    content.message.Add(new RecordMessage(Music.MusicList[match_music.First()]));
                    await Bot.SendGroupMessage(e.group_id, "Record", content);
                    return quick_reply;
                }

                // 我的运势
                if (e.detail == "我的运势")
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;

                    UserDaily daily = await Bot.HttpPost<UserDaily>("https://api.milimoe.com/userdaily/get/" + e.user_id, "") ?? new(0, 0, "");
                    if (daily.daily != "")
                    {
                        if (daily.type == 0)
                        {
                            GroupMessageContent content = new(e.group_id);
                            content.message.Add(new AtMessage(e.user_id));
                            content.message.Add(new TextMessage(daily.daily));
                            await Bot.SendGroupMessage(e.group_id, "我的运势", content);
                        }
                        else
                        {
                            string img = "file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\zi\";
                            img += daily.type switch
                            {
                                1 => "dj" + (new Random().Next(3) + 1) + ".png",
                                2 => "zj" + (new Random().Next(2) + 1) + ".png",
                                3 => "j" + (new Random().Next(4) + 1) + ".png",
                                4 => "mj" + (new Random().Next(2) + 1) + ".png",
                                5 => "x" + (new Random().Next(2) + 1) + ".png",
                                6 => "dx" + (new Random().Next(2) + 1) + ".png",
                                _ => ""
                            };

                            GroupMessageContent content = new(e.group_id);
                            content.message.Add(new AtMessage(e.user_id));
                            content.message.Add(new TextMessage(daily.daily));
                            await Bot.SendGroupMessage(e.group_id, "我的运势", content);

                            content = new(e.group_id);
                            content.message.Add(new ImageMessage(img));
                            await Bot.SendGroupMessage(e.group_id, "我的运势配图", content);
                        }
                    }

                    return quick_reply;
                }
                if (e.detail == "重置运势")
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;

                    string msg = await Bot.HttpPost<string>("https://api.milimoe.com/userdaily/remove/" + e.user_id, "") ?? "";
                    if (msg != "")
                    {
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new AtMessage(e.user_id));
                        content.message.Add(new TextMessage(msg));
                        await Bot.SendGroupMessage(e.group_id, "重置运势", content);
                    }

                    return quick_reply;
                }
                if (e.detail.Length > 4 && e.detail[..2] == "查看" && (e.detail[^2..] == "运势"))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    string[] strs = e.detail.Replace("查看", "").Replace("运势", "").Trim().Split(' ');
                    foreach (string str_qq in strs)
                    {
                        if (long.TryParse(str_qq.Trim().Replace("@", ""), out long qq))
                        {
                            if (qq == GeneralSettings.BotQQ)
                            {
                                await Bot.HttpPost<UserDaily>("https://api.milimoe.com/userdaily/get/" + qq, "");
                            }
                            UserDaily daily = await Bot.HttpGet<UserDaily>("https://api.milimoe.com/userdaily/view/" + qq) ?? new(0, 0, "");
                            if (daily.daily != "")
                            {
                                GroupMessageContent content = new(e.group_id);
                                content.message.Add(new AtMessage(e.user_id));
                                content.message.Add(new TextMessage(daily.daily));
                                await Bot.SendGroupMessage(e.group_id, "查看运势", content);
                            }
                        }
                    }
                    return quick_reply;
                }
                if (e.user_id == GeneralSettings.Master && e.detail.Length > 4 && e.detail[..2] == "重置" && (e.detail[^2..] == "运势"))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    string[] strs = e.detail.Replace("重置", "").Replace("运势", "").Trim().Split(' ');
                    foreach (string str_qq in strs)
                    {
                        if (long.TryParse(str_qq.Trim().Replace("@", ""), out long qq))
                        {
                            string msg = await Bot.HttpPost<string>("https://api.milimoe.com/userdaily/remove/" + e.user_id, "") ?? "";
                            if (msg != "")
                            {
                                await Bot.SendGroupMessage(e.group_id, "重置运势", "已重置" + Bot.GetMemberNickName(e.group_id, qq) + "（" + qq + "）的今日运势。");
                            }
                        }
                    }
                    return quick_reply;
                }

                // 下面是开启了OSM Core的群组才能使用的功能
                if (!onOSMCore) return quick_reply;

                // 禁言抽奖
                if (GeneralSettings.IsMute && e.detail == "禁言抽奖" && !MuteRecall.WillMute.ContainsKey(e.user_id))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id) || !Bot.BotIsAdmin(e.group_id)) return quick_reply;
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
                        await Bot.SendGroupMessage(e.group_id, "禁言抽奖", "我不能禁言主人！");
                    }
                    return quick_reply;
                }
                // 忏悔
                else if (GeneralSettings.IsMute && e.detail == "忏悔" && MuteRecall.WillMute.ContainsKey(e.user_id))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id) || !Bot.BotIsAdmin(e.group_id)) return quick_reply;
                    await Task.Delay(3800);
                    MuteRecall.WillMute.Remove(e.user_id);
                    await Bot.SendMessage(SupportedAPI.set_group_ban, e.group_id, "忏悔", new SetGroupBanContent(e.group_id, e.user_id, 0), true);
                    await Bot.SendGroupMessage(e.group_id, "忏悔", "忏悔成功！！希望你保持纯真，保持野性的美。");
                    return quick_reply;
                }

                // 随机反驳是
                if (e.detail == "是")
                {
                    if (e.user_id != GeneralSettings.Master && e.CheckThrow(40, out dice))
                    {
                        Bot.ColorfulCheckPass(sender, "反驳是", dice, 40);
                        await Bot.SendGroupMessage(e.group_id, "随机反驳是", "是你的头");
                    }
                    else if (e.user_id == GeneralSettings.Master)
                    {
                        await Bot.SendGroupMessage(e.group_id, "随机反驳是", "是你的头");
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
                        await Bot.SendGroupMessage(e.group_id, "随机反驳不", content);
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
                        await Bot.SendGroupMessage(e.group_id, "随机反驳不", content);
                    }
                }
                else if (e.detail.Contains("可以") && !e.detail.Contains('不') && e.CheckThrow(GeneralSettings.PSayNo, out dice))
                {
                    Bot.ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                    if (dice < (GeneralSettings.PSayNo / 2))
                    {
                        await Bot.SendGroupMessage(e.group_id, "随机反驳不", "可以");
                    }
                    else
                    {
                        await Bot.SendGroupMessage(e.group_id, "随机反驳不", "不可以");
                    }
                }
                else if (e.detail.Contains('能') && !e.detail.Contains('不') && !SayNo.IgnoreTriggerBeforeCan.Any(e.detail.Contains) && e.CheckThrow(GeneralSettings.PSayNo, out dice))
                {
                    Bot.ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                    if (dice < (GeneralSettings.PSayNo / 2))
                    {
                        await Bot.SendGroupMessage(e.group_id, "随机反驳不", "能");
                    }
                    else
                    {
                        await Bot.SendGroupMessage(e.group_id, "随机反驳不", "不能");
                    }
                }
                else if (e.detail.Contains("可能") && !e.detail.Contains('不') && e.CheckThrow(GeneralSettings.PSayNo, out dice))
                {
                    Bot.ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                    if (dice < (GeneralSettings.PSayNo / 2))
                    {
                        await Bot.SendGroupMessage(e.group_id, "随机反驳不", "可能");
                    }
                    else
                    {
                        await Bot.SendGroupMessage(e.group_id, "随机反驳不", "不可能");
                    }
                }
                else if (e.detail.Contains('要') && !e.detail.Contains('不') && e.CheckThrow(GeneralSettings.PSayNo, out dice))
                {
                    Bot.ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                    await Bot.SendGroupMessage(e.group_id, "随机反驳不", SayNo.SayWantWords[new Random().Next(SayNo.SayWantWords.Count)]);
                }
                else if (e.detail.Contains('想') && !e.detail.Contains('不') && e.CheckThrow(GeneralSettings.PSayNo, out dice))
                {
                    Bot.ColorfulCheckPass(sender, "随机反驳不", dice, GeneralSettings.PSayNo);
                    await Bot.SendGroupMessage(e.group_id, "随机反驳不", SayNo.SayThinkWords[new Random().Next(SayNo.SayThinkWords.Count)]);
                }

                // 反向艾特
                IEnumerable<AtMessage> temp_at = e.message.Where(m => m.type == "at").Cast<AtMessage>().Where(m => m.data.qq == $"{GeneralSettings.BotQQ}");
                if (temp_at.Any())
                {
                    if (GeneralSettings.IsReverseAt && !Ignore.ReverseAtIgnore.Contains(e.user_id) && e.CheckThrow(GeneralSettings.PReverseAt, out dice))
                    {
                        Bot.ColorfulCheckPass(sender, "反向艾特", dice, GeneralSettings.PReverseAt);
                        foreach (AtMessage at in temp_at)
                        {
                            at.data.qq = e.user_id.ToString();
                            GroupMessageContent content = new(e.group_id);
                            content.message.AddRange(e.message);
                            await Bot.SendGroupMessage(e.group_id, "反向艾特", content);
                        }
                    }
                    return quick_reply;
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
                    await Bot.SendGroupMessage(e.group_id, "Image", content);
                    return quick_reply;
                }

                // 随机复读
                if (GeneralSettings.IsRepeat && !Ignore.RepeatIgnore.Any(e.detail.Contains) && !Ignore.RepeatQQIgnore.Contains(e.user_id) && e.CheckThrow(GeneralSettings.PRepeat, out dice))
                {
                    // 出现了@at就直接触发复读，没有延迟
                    int delay = e.message.Where(m => m.type == "at").Any() ? 0 : GeneralSettings.RepeatDelay[0] + new Random().Next(GeneralSettings.RepeatDelay[1] - GeneralSettings.RepeatDelay[0]);
                    Bot.ColorfulCheckPass(sender, "随机复读", dice, GeneralSettings.PRepeat, delay);
                    GroupMessageContent content = new(e.group_id);
                    content.message.AddRange(e.message);
                    await Bot.SendGroupMessage(e.group_id, "随机复读", content, delay * 1000);
                    return quick_reply;
                }

                // 随机叫哥
                if (GeneralSettings.IsCallBrother && !Ignore.CallBrotherQQIgnore.Contains(e.user_id) && e.CheckThrow(GeneralSettings.PCallBrother, out dice))
                {
                    int delay = GeneralSettings.RepeatDelay[0] + new Random().Next(GeneralSettings.RepeatDelay[1]);
                    Bot.ColorfulCheckPass(sender, "随机叫哥", dice, GeneralSettings.PCallBrother, delay);
                    string name = (sender.card != "" ? sender.card : sender.nickname).Trim();
                    int pos = new Random().Next(name.Length - 1);
                    if (pos != -1)
                    {
                        GroupMessageContent content = new(e.group_id);
                        content.message.Add(new AtMessage(e.user_id));
                        switch (Random.Shared.Next(4))
                        {
                            case 0:
                                content.message.Add(new TextMessage(string.Concat(name.AsSpan(pos, name.Length > 1 ? 2 : name.Length), "哥")));
                                break;
                            case 1:
                                content.message.Add(new TextMessage(string.Concat(name.AsSpan(pos, name.Length > 1 ? 2 : name.Length), "姐")));
                                break;
                            case 2:
                                content.message.Add(new TextMessage(string.Concat(name.AsSpan(pos, name.Length > 0 ? 1 : name.Length), "圣")));
                                break;
                            case 3:
                            default:
                                content.message.Add(new TextMessage(string.Concat(name.AsSpan(pos, name.Length > 0 ? 1 : name.Length), "出")));
                                break;
                        }
                        await Bot.SendGroupMessage(e.group_id, "随机叫哥", content, delay * 1000);
                        return quick_reply;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            return quick_reply;
        }
    }
}
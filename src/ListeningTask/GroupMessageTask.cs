using Milimoe.OneBot.Model.Content;
using Milimoe.OneBot.Model.Event;
using Milimoe.OneBot.Model.Message;
using Milimoe.OneBot.Model.Other;
using Milimoe.OneBot.Model.QuickReply;
using Milimoe.RainBOT.Settings;

namespace Milimoe.RainBOT.ListeningTask
{
    public class GroupMessageTask
    {
        private static long dice = 0;

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

                if (e.detail.StartsWith("查询服务器启动时间"))
                {
                    string msg = "";
                    string local = "";
                    try
                    {
                        msg = await Bot.HttpGet<string>("http://192.168.1.6/test/getlastlogintime/") ?? "";
                        local = msg;
                    }
                    catch (Exception ex)
                    {
                        local = "无法连接至发布服务器，原因：" + ex.Message;
                    }
                    try
                    {
                        msg = await Bot.HttpGet<string>("http://localhost:5000/test/getlastlogintime/") ?? "";
                        local += "\r\n检测备用服务器，" + msg;
                    }
                    catch { }
                    await Bot.SendGroupMessage(e.group_id, "查询服务器启动时间", local);
                }
                
                // 随机反驳是
                if (e.detail == "是" && e.CheckThrow(40, out dice))
                {
                    Bot.ColorfulCheckPass(sender, "反驳是", dice, 40);
                    await Bot.SendGroupMessage(e.group_id, "随机反驳是", "是你的头");
                }
                
                // 随机反驳不
                if (SayNo.Trigger.Any(e.detail.Contains) && e.CheckThrow(GeneralSettings.PSayNo, out dice))
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
                else if (SayNo.TriggerBeforeNo.Any(e.detail.Contains) && e.CheckThrow(GeneralSettings.PSayNo, out dice))
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
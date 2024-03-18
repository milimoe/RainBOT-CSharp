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
    public class FriendMessageTask
    {
        private static long dice = 0;

        public static void ListeningTask_handler(FriendMessageEvent e, out FriendMsgEventQuickReply? quick_reply)
        {
            quick_reply = null;
            try
            {
                Sender sender = e.sender;
                if (e.user_id == 0 || e.sender.user_id == 0) return;

                Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} P/{e.user_id}{(e.detail.Trim() == "" ? "" : " -> " + e.detail)}");
                if (GeneralSettings.IsDebug)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"DEBUG：{e.original_msg}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                if (e.detail == "是")
                {
                    if (e.user_id != GeneralSettings.Master && e.CheckThrow(10, out dice))
                    {
                        ColorfulCheckPass(sender, "反驳是", dice, 40);
                        _ = Post(e, "随机反驳是", "是你的头");
                    }
                    else if (e.user_id == GeneralSettings.Master)
                    {
                        _ = Post(e, "随机反驳是", "是你的头");
                    }
                }

                // OSM指令
                if (e.detail.Length >= 4 && e.detail[..4] == ".osm")
                {
                    if (GeneralSettings.IsRun && e.detail.Contains(".osm info"))
                    {
                        // OSM核心状态
                        string msg = "OSM插件运行状态：" + "\r\n本群已启用OSM核心";
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
                    else if (e.detail.Contains(".osm refresh"))
                    {
                        if (e.user_id == GeneralSettings.Master)
                        {
                            TaskUtility.NewTask(async () =>
                            {
                                await Bot.GetGroups();
                                await Bot.GetGroupMembers();
                                await Post(e, "OSM指令", "刷新缓存完成。");
                            });
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
                    else if (e.detail.Contains(".osm send"))
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
                                _ = Post(SupportedAPI.send_group_msg, group_id, "OSM指令", content);
                            }
                        }
                        else _ = Post(e, "OSM指令", "你没有权限使用此指令。");
                    }
                    else if (e.detail.Contains(".osm sendall"))
                    {
                        if (e.user_id == GeneralSettings.Master)
                        {
                            string str = e.detail.Replace(".osm send", "").Trim();
                            string[] strs = Regex.Split(str, @"\s+");
                            string result = MasterCommand.Execute(".osm sendall", strs[0]);
                            foreach (long group_id in Bot.Groups.Select(g => g.group_id))
                            {
                                GroupMessageContent content = new(group_id);
                                content.message.Add(new TextMessage(result));
                                _ = Post(SupportedAPI.send_group_msg, group_id, "OSM指令", content);
                            }
                        }
                        else _ = Post(e, "OSM指令", "你没有权限使用此指令。");
                    }
                    else
                    {
                        // OSM核心信息
                        FriendMessageContent content = new(e.user_id);
                        content.message.Add(new TextMessage($"OSM Core {OSMCore.version} {OSMCore.version2}\r\nAuthor: Milimoe\r\nBuilt on {OSMCore.time}\r\nSee: https://github.com/milimoe"));
                        content.message.Add(new ImageMessage("file:///" + AppDomain.CurrentDomain.BaseDirectory.ToString() + @"img\raincandy.jpg"));
                        _ = Post(e, "OSM核心", content);
                        return;
                    }
                    return;
                }

                if (GeneralSettings.IsMute && e.detail == "忏悔")
                {
                    TaskUtility.NewTask(async () =>
                    {
                        if (!await CheckBlackList(e)) return;
                        string msg = "";
                        foreach (long group_id in Bot.Groups.Select(g => g.group_id))
                        {
                            if (Bot.BotIsAdmin(group_id) && MuteRecall.Muted[group_id].TryGetValue(e.user_id, out long operator_id) && operator_id == Bot.BotQQ)
                            {
                                MuteRecall.Muted[group_id].Remove(e.user_id);
                                await GroupMessageTask.Post(SupportedAPI.set_group_ban, group_id, "忏悔", new SetGroupBanContent(group_id, e.user_id, 0));
                                if (msg != "") msg += "\r\n";
                                msg += $"[{group_id}] 忏悔成功！！希望你保持纯真，保持野性的美。";
                            }
                        }
                        if (msg == "") msg = "你无需忏悔。请注意：我不能帮你解除由管理员手动操作的禁言。";
                        await Post(e, "忏悔", msg);
                    });
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

        public static async Task<bool> CheckBlackList(FriendMessageEvent e)
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
                FriendMessageContent content = new(e.user_id);
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

        public static async Task Post(FriendMessageEvent e, string function, string text, int delay = 0)
        {
            string result = (await e.SendMessage(text, delay)).ReasonPhrase ?? "";
            Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} F/");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(function);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($" P/{e.user_id} <- {text} {result}");
        }

        public static async Task Post(FriendMessageEvent e, string function, FriendMessageContent content, int delay = 0)
        {
            string result = (await e.SendMessage(content, delay)).ReasonPhrase ?? "";
            Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} F/");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(function);
            Console.ForegroundColor = ConsoleColor.Gray;
            if (!GeneralSettings.IsDebug)
            {
                Console.WriteLine($" P/{e.user_id} <- {content.detail} {result}");
            }
            else
            {
                Console.WriteLine($" P/{e.user_id} <- {JsonTools.GetString(content)} {result}");
            }
        }

        public static async Task Post(string api, long user_id, string function, IContent content)
        {
            string result = (await HTTPPost.Post(api, content)).ReasonPhrase ?? "";
            Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} F/");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(function);
            Console.ForegroundColor = ConsoleColor.Gray;
            if (!GeneralSettings.IsDebug)
            {
                Console.WriteLine($" P/{user_id} <- {content.detail} {result}");
            }
            else
            {
                Console.WriteLine($" P/{user_id} <- {HTTPHelper.GetJsonString(api, content)} {result}");
            }
        }

        public static async Task Post(string api, long user_id, string function, IEnumerable<IContent> contents)
        {
            await HTTPPost.Post(api, contents);
            Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} F/");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(function);
            Console.ForegroundColor = ConsoleColor.Gray;
            if (!GeneralSettings.IsDebug)
            {
                Console.WriteLine($" P/{user_id} <- 已在后台执行");
            }
            else
            {
                Console.WriteLine($" P/{user_id} <- 已在后台执行");
            }
        }
    }
}
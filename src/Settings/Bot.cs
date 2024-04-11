using System.Text.Json;
using System.Text.RegularExpressions;
using Milimoe.OneBot.Framework;
using Milimoe.OneBot.Framework.Interface;
using Milimoe.OneBot.Framework.Utility;
using Milimoe.OneBot.Model.Content;
using Milimoe.OneBot.Model.Message;
using Milimoe.OneBot.Model.Other;
using Milimoe.OneBot.Utility;
using Milimoe.RainBOT.Command;
using Group = Milimoe.OneBot.Model.Other.Group;

namespace Milimoe.RainBOT.Settings
{
    public class Bot
    {
        public static long BotQQ => GeneralSettings.BotQQ;

        public static bool BotIsAdmin(long group_id) => IsAdmin(group_id, BotQQ);

        public static List<Group> Groups { get; set; } = [];

        public static Dictionary<long, List<Member>> GroupMembers { get; set; } = [];

        public static bool IsAdmin(long group_id, long user_id)
        {
            if (GroupMembers.TryGetValue(group_id, out List<Member>? members) && members != null && members.Any(m => m.user_id == user_id && (m.role == "owner" || m.role == "admin")))
            {
                return true;
            }
            return false;
        }

        public static async Task GetGroups()
        {
            HttpResponseMessage msg = await HTTPPost.Post(SupportedAPI.get_group_list, new GroupMessageContent(0));
            if (msg.IsSuccessStatusCode)
            {
                JsonDocument jsonDocument = JsonDocument.Parse(await msg.Content.ReadAsStringAsync());
                JsonElement data = jsonDocument.RootElement.GetProperty("data");
                Groups = data.Deserialize<List<Group>>(JsonTools.options) ?? [];
            }
            MuteRecall.Muted.Clear();
            foreach (Group g in Groups)
            {
                MuteRecall.Muted.Add(g.group_id, []);
            }
        }

        public static async Task GetGroupMembers()
        {
            GroupMembers.Clear();
            foreach (Group g in Groups)
            {
                HttpResponseMessage msg = await HTTPPost.Post(SupportedAPI.get_group_member_list, new GetGroupMemberListContent(g.group_id));
                if (msg.IsSuccessStatusCode)
                {
                    JsonDocument jsonDocument = JsonDocument.Parse(await msg.Content.ReadAsStringAsync());
                    JsonElement data = jsonDocument.RootElement.GetProperty("data");
                    GroupMembers.Add(g.group_id, data.Deserialize<List<Member>>(JsonTools.options) ?? []);
                }
            }
        }

        public static Member GetMember(long group_id, long user_id)
        {
            if (GroupMembers.TryGetValue(group_id, out List<Member>? members) && members != null)
            {
                Member? member = members.Where(m => m.user_id == user_id).FirstOrDefault();
                if (member != null)
                {
                    return member;
                }
            }
            return new();
        }

        public static string GetMemberNickName(long group_id, long user_id)
        {
            Member member = GetMember(group_id, user_id);
            if (member.user_id != 0) return member.card != "" ? member.card : member.nickname;
            return "";
        }

        public static string GetMemberNickName(Member member)
        {
            return member.card != "" ? member.card : member.nickname;
        }

        public static async Task<bool> CheckBlackList(bool send_group, long user_id, long target_id)
        {
            // 黑名单
            if (user_id == GeneralSettings.Master) return true;
            if (!BlackList.Times.ContainsKey(user_id))
            {
                BlackList.Times.Add(user_id, 1);
                return true;
            }
            else if (BlackList.Times.TryGetValue(user_id, out long bltimes) && bltimes > 5)
            {
                return false;
            }
            else if (++bltimes == 5)
            {
                BlackList.Times[user_id] = 6;
                FriendMessageContent content = new(user_id);
                content.message.Add(new AtMessage(user_id));
                content.message.Add(new TextMessage("警告：你已因短时间内频繁操作被禁止使用BOT指令" + (GeneralSettings.BlackFrozenTime / 60) + "分钟" + (GeneralSettings.BlackFrozenTime % 60) + "秒。"));
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000 * GeneralSettings.BlackFrozenTime);
                    BlackList.Times.Remove(user_id);
                });
                await (send_group ? SendGroupMessage(target_id, "黑名单", content) : SendFriendMessage(target_id, "黑名单", content));
                return false;
            }
            else
            {
                BlackList.Times[user_id] = bltimes;
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

        public static async Task SendGroupMessage(long group_id, string function, string text, int delay = 0)
        {
            GroupMessageContent content = new(group_id);
            content.message.Add(new TextMessage(text));
            if (delay > 0)
            {
                await Task.Delay(delay);
            }
            await SendMessage(SupportedAPI.send_group_msg, group_id, function, content, true);
        }

        public static async Task SendGroupMessage(long group_id, string function, IContent content) => await SendMessage(SupportedAPI.send_group_msg, group_id, function, content, true);

        public static async Task SendGroupMessage(long group_id, string function, IContent content, int delay = 0)
        {
            if (delay > 0)
            {
                await Task.Delay(delay);
            }
            await SendMessage(SupportedAPI.send_group_msg, group_id, function, content, true);
        }

        public static async Task SendGroupMessage(long group_id, string function, IEnumerable<IContent> contents) => await SendMessage(SupportedAPI.send_group_msg, group_id, function, contents, true);

        public static async Task SendFriendMessage(long user_id, string function, string text)
        {
            FriendMessageContent content = new(user_id);
            content.message.Add(new TextMessage(text));
            await SendMessage(SupportedAPI.send_private_msg, user_id, function, content, false);
        }

        public static async Task SendFriendMessage(long user_id, string function, IContent content) => await SendMessage(SupportedAPI.send_private_msg, user_id, function, content, false);

        public static async Task SendFriendMessage(long user_id, string function, IEnumerable<IContent> contents) => await SendMessage(SupportedAPI.send_private_msg, user_id, function, contents, false);

        public static async Task SendMessage(string api, long target_id, string function, IContent content, bool send_group)
        {
            string msg_type = send_group ? "G" : "P";
            string result = (await HTTPPost.Post(api, content)).ReasonPhrase ?? "";
            Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} F/");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(function);
            Console.ForegroundColor = ConsoleColor.Gray;
            if (!GeneralSettings.IsDebug)
            {
                Console.WriteLine($" {msg_type}/{target_id} <- {content.detail} {result}");
            }
            else
            {
                Console.WriteLine($" {msg_type}/{target_id} <- {HTTPHelper.GetJsonString(api, content)} {result}");
            }
        }

        public static async Task SendMessage(string api, long target_id, string function, IEnumerable<IContent> contents, bool send_group)
        {
            string msg_type = send_group ? "G" : "P";
            await HTTPPost.Post(api, contents);
            Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} F/");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(function);
            Console.ForegroundColor = ConsoleColor.Gray;
            if (!GeneralSettings.IsDebug)
            {
                Console.WriteLine($" {msg_type}/{target_id} <- 已在后台执行");
            }
            else
            {
                Console.WriteLine($" {msg_type}/{target_id} <- 已在后台执行");
            }
        }

        public static async Task Mute(long user_id, long group_id, string detail)
        {
            if (!BotIsAdmin(group_id)) return;
            bool unmute = detail.Contains("解禁");
            string[] strs = Regex.Split(detail, @"\s+");
            if (!unmute && strs.Length < 2) return;
            if (user_id == GeneralSettings.Master || (unmute && GeneralSettings.UnMuteAccessGroup.Contains(user_id)) || (!unmute && GeneralSettings.MuteAccessGroup.Contains(user_id)))
            {
                strs = Regex.Split(detail.Replace(".osm mute", "").Replace("禁言", "").Replace("解禁", "").Replace("所有人", "").Trim(), @"\s+");
                long time = 0;
                if ((!unmute && strs.Length > 1 && long.TryParse(strs[^1], out time) && time >= 0 && time < 2592000) || unmute)
                {
                    List<long> qqlist = [];
                    List<IContent> list = [];
                    foreach (string str in unmute ? strs : strs[..^1])
                    {
                        if (long.TryParse(str.Replace(@"@", "").Trim(), out long qq))
                        {
                            SetGroupBanContent content = new(group_id, qq, time);
                            list.Add(content);
                            qqlist.Add(qq);
                        }
                    }
                    await SendMessage(SupportedAPI.set_group_ban, group_id, "禁言指令", list, true);
                    if (time > 0)
                    {
                        await Task.Delay(3000);
                        foreach (long qq in qqlist)
                        {
                            if (MuteRecall.Muted[group_id].ContainsKey(qq)) MuteRecall.Muted[group_id][qq] = GeneralSettings.Master;
                            else MuteRecall.Muted[group_id].Add(qq, GeneralSettings.Master);
                        }
                    }
                    return;
                }
                await SendGroupMessage(group_id, "OSM指令", MasterCommand.Execute_Worker(".osm mute", "", strs.Length > 1 ? strs[1..] : []));
            }
            else await SendGroupMessage(group_id, "OSM指令", "你没有权限使用此指令。");
        }

        public static async Task Mute(long user_id, long group_id, string detail, IEnumerable<long> qqlist)
        {
            if (!BotIsAdmin(group_id)) return;
            bool unmute = detail.Contains("解禁");
            if (user_id == GeneralSettings.Master || (unmute && GeneralSettings.UnMuteAccessGroup.Contains(user_id)) || (!unmute && GeneralSettings.MuteAccessGroup.Contains(user_id)))
            {
                string[] strs = Regex.Split(detail.Replace("禁言", "").Replace("解禁", "").Replace("所有人", "").Trim(), @"\s+");
                long mute_time = unmute ? 0 : GeneralSettings.MuteTime[0] + new Random().NextInt64(GeneralSettings.MuteTime[1] - GeneralSettings.MuteTime[0]);
                if (long.TryParse(strs[^1], out long time) && time >= 0 && time < 2592000)
                {
                    mute_time = time;
                }
                List<IContent> list = [];
                foreach (long qq in qqlist)
                {
                    SetGroupBanContent content = new(group_id, qq, mute_time);
                    list.Add(content);
                }
                await SendMessage(SupportedAPI.set_group_ban, group_id, "批量禁言指令", list, true);
                if (mute_time > 0)
                {
                    await Task.Delay(3000);
                    foreach (long qq in qqlist)
                    {
                        if (MuteRecall.Muted[group_id].ContainsKey(qq)) MuteRecall.Muted[group_id][qq] = GeneralSettings.Master;
                        else MuteRecall.Muted[group_id].Add(qq, GeneralSettings.Master);
                    }
                }
            }
            else await SendGroupMessage(group_id, "OSM指令", "你没有权限使用此指令。");
        }

        public static async Task MuteGroup(long user_id, long group_id, string detail)
        {
            if (!BotIsAdmin(group_id)) return;
            if (user_id == GeneralSettings.Master || GeneralSettings.MuteAccessGroup.Contains(user_id))
            {
                string str = detail.Replace(".osm mutegroup", "").Replace("跨群禁言", "").Trim();
                string[] strs = Regex.Split(str, @"\s+");
                if (strs.Length > 2)
                {
                    string str_group = strs[0].Replace(@"@", "").Trim();
                    string str_qq = strs[1].Replace(@"@", "").Trim();
                    if (long.TryParse(str_group, out long group) && long.TryParse(str_qq, out long qq) && long.TryParse(strs[2], out long time) && time >= 0 && time < 2592000)
                    {
                        SetGroupBanContent content = new(group, qq, time);
                        await SendMessage(SupportedAPI.set_group_ban, group, "OSM指令", content, true);
                        if (time > 0)
                        {
                            await Task.Delay(3000);
                            if (MuteRecall.Muted[group_id].ContainsKey(qq)) MuteRecall.Muted[group_id][qq] = GeneralSettings.Master;
                            else MuteRecall.Muted[group_id].Add(qq, GeneralSettings.Master);
                        }
                    }
                    else await SendGroupMessage(group, "OSM指令", MasterCommand.Execute_Worker(".osm mute", "", strs.Length > 1 ? strs[1..] : []));
                }
                else await SendGroupMessage(group_id, "OSM指令", MasterCommand.Execute_Worker(".osm mute", "", strs.Length > 1 ? strs[1..] : []));
            }
            else await SendGroupMessage(group_id, "OSM指令", "你没有权限使用此指令。");
        }
    }
}

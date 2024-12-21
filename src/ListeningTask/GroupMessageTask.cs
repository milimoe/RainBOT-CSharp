using System.Text.RegularExpressions;
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
        private readonly static List<string> FunGameItemType = ["卡包", "武器", "防具", "鞋子", "饰品", "消耗品", "魔法卡", "收藏品", "特殊物品", "任务物品", "礼包", "其他"];

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

                if (!GeneralSettings.IsRun)
                {
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

                if (e.detail.Length >= 9 && e.detail[..9].Equals("FunGame模拟", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    if (!Bot.FunGameSimulation)
                    {
                        Bot.FunGameSimulation = true;
                        List<string> msgs = await Bot.HttpGet<List<string>>("https://api.milimoe.com/fungame/test?isweb=false") ?? [];
                        List<string> real = [];
                        int remain = 7;
                        string merge = "";
                        for (int i = 0; i < msgs.Count - 2; i++)
                        {
                            remain--;
                            merge += msgs[i] + "\r\n";
                            if (remain == 0)
                            {
                                real.Add(merge);
                                merge = "";
                                if ((msgs.Count - i - 3) < 7)
                                {
                                    remain = msgs.Count - i - 3;
                                }
                                else remain = 7;
                            }
                        }
                        if (msgs.Count > 2)
                        {
                            real.Add(msgs[^2]);
                            real.Add(msgs[^1]);
                        }
                        foreach (string msg in real)
                        {
                            await Bot.SendGroupMessage(e.group_id, "FunGame模拟", msg.Trim());
                            await Task.Delay(5500);
                        }
                        Bot.FunGameSimulation = false;
                    }
                    else
                    {
                        await Bot.SendGroupMessage(e.group_id, "FunGame模拟", "游戏正在模拟中，请勿重复请求！");
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 11 && e.detail[..11].Equals("FunGame团队模拟", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    if (!Bot.FunGameSimulation)
                    {
                        Bot.FunGameSimulation = true;
                        List<string> msgs = await Bot.HttpGet<List<string>>("https://api.milimoe.com/fungame/test?isweb=false&isteam=true") ?? [];
                        List<string> real = [];
                        if (msgs.Count > 0)
                        {
                            real.Add(msgs[0]);
                        }
                        int remain = 7;
                        string merge = "";
                        for (int i = 1; i < msgs.Count - 2; i++)
                        {
                            remain--;
                            merge += msgs[i] + "\r\n";
                            if (remain == 0)
                            {
                                real.Add(merge);
                                merge = "";
                                if ((msgs.Count - i - 3) < 7)
                                {
                                    remain = msgs.Count - i - 3;
                                }
                                else remain = 7;
                            }
                        }
                        if (msgs.Count > 2)
                        {
                            real.Add(msgs[^2]);
                            real.Add(msgs[^1]);
                        }
                        foreach (string msg in real)
                        {
                            await Bot.SendGroupMessage(e.group_id, "FunGame团队模拟", msg.Trim());
                            await Task.Delay(5500);
                        }
                        Bot.FunGameSimulation = false;
                    }
                    else
                    {
                        await Bot.SendGroupMessage(e.group_id, "FunGame团队模拟", "游戏正在模拟中，请勿重复请求！");
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 3 && e.detail[..3].Equals("查数据", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    string detail = e.detail.Replace("查数据", "").Trim();
                    if (int.TryParse(detail, out int id))
                    {
                        string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/fungame/stats?id=" + id) ?? "").Trim();
                        if (msg != "")
                        {
                            await Bot.SendGroupMessage(e.group_id, "查询FunGame数据", msg);
                        }
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 5 && e.detail[..5].Equals("查团队数据", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    string detail = e.detail.Replace("查团队数据", "").Trim();
                    if (int.TryParse(detail, out int id))
                    {
                        string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/fungame/teamstats?id=" + id) ?? "").Trim();
                        if (msg != "")
                        {
                            await Bot.SendGroupMessage(e.group_id, "查询FunGame数据", msg);
                        }
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 5 && e.detail[..5].Equals("查个人胜率", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    string[] msg = await Bot.HttpGet<string[]>("https://api.milimoe.com/fungame/winraterank?isteam=false") ?? [];
                    if (msg.Length > 0)
                    {
                        await Bot.SendGroupMessage(e.group_id, "查询FunGame数据", string.Join("\r\n\r\n", msg));
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 5 && e.detail[..5].Equals("查团队胜率", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    string[] msg = await Bot.HttpGet<string[]>("https://api.milimoe.com/fungame/winraterank?isteam=true") ?? [];
                    if (msg.Length > 0)
                    {
                        await Bot.SendGroupMessage(e.group_id, "查询FunGame数据", string.Join("\r\n\r\n", msg));
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 3 && e.detail[..3].Equals("查角色", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    string detail = e.detail.Replace("查角色", "").Trim();
                    if (int.TryParse(detail, out int id))
                    {
                        string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/fungame/characterinfo?id=" + id) ?? "").Trim();
                        if (msg != "")
                        {
                            await Bot.SendGroupMessage(e.group_id, "查询FunGame角色技能", msg);
                        }
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 3 && e.detail[..3].Equals("查技能", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    string detail = e.detail.Replace("查技能", "").Trim();
                    if (int.TryParse(detail, out int id))
                    {
                        string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/fungame/skillinfo?id=" + id) ?? "").Trim();
                        if (msg != "")
                        {
                            await Bot.SendGroupMessage(e.group_id, "查询FunGame角色技能", msg);
                        }
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 3 && e.detail[..3].Equals("查物品", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    string detail = e.detail.Replace("查物品", "").Trim();
                    if (int.TryParse(detail, out int id))
                    {
                        string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/fungame/iteminfo?id=" + id) ?? "").Trim();
                        if (msg != "")
                        {
                            await Bot.SendGroupMessage(e.group_id, "查询FunGame物品信息", msg);
                        }
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 6 && e.detail[..6] == "生成魔法卡包")
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/fungame/newmagiccardpack") ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "生成魔法卡包", msg);
                    }
                    return quick_reply;
                }
                else if (e.detail.Length >= 5 && e.detail[..5] == "生成魔法卡")
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/fungame/newmagiccard") ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "生成魔法卡", msg);
                    }
                    return quick_reply;
                }

                if (e.detail == "创建存档")
                {
                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/createsaved?qq={e.user_id}", "") ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessageAt(e.user_id, e.group_id, "创建存档", "\r\n" + msg);
                    }
                    return quick_reply;
                }
                
                if (e.detail == "还原存档")
                {
                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/restoresaved?qq={e.user_id}", "") ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessageAt(e.user_id, e.group_id, "还原存档", "\r\n" + msg);
                    }
                    return quick_reply;
                }
                
                if (e.detail == "生成自建角色")
                {
                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/newcustomcharacter?qq={e.user_id}", "") ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessageAt(e.user_id, e.group_id, "抽卡", "\r\n" + msg);
                    }
                    return quick_reply;
                }
                
                if (e.detail == "角色改名")
                {
                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/rename?qq={e.user_id}", "") ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessageAt(e.user_id, e.group_id, "改名", "\r\n" + msg);
                    }
                    return quick_reply;
                }
                
                if (e.detail == "角色重随")
                {
                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/randomcustom?qq={e.user_id}&confirm=false", "") ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessageAt(e.user_id, e.group_id, "角色重随", "\r\n" + msg);
                    }
                    return quick_reply;
                }
                
                if (e.detail == "确认角色重随")
                {
                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/randomcustom?qq={e.user_id}&confirm=true", "") ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessageAt(e.user_id, e.group_id, "角色重随", "\r\n" + msg);
                    }
                    return quick_reply;
                }
                
                if (e.detail == "取消角色重随")
                {
                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/cancelrandomcustom?qq={e.user_id}", "") ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessageAt(e.user_id, e.group_id, "角色重随", "\r\n" + msg);
                    }
                    return quick_reply;
                }
                
                if (e.detail == "抽卡")
                {
                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/drawcard?qq={e.user_id}", "") ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessageAt(e.user_id, e.group_id, "抽卡", "\r\n" + msg);
                    }
                    return quick_reply;
                }
                
                if (e.detail == "十连抽卡")
                {
                    List<string> msgs = (await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/drawcards?qq={e.user_id}", "") ?? []);
                    if (msgs.Count > 0)
                    {
                        await Bot.SendGroupMessageAt(e.user_id, e.group_id, "十连抽卡", "\r\n" + string.Join("\r\n", msgs));
                    }
                    return quick_reply;
                }
                
                if (e.detail == "材料抽卡")
                {
                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/drawcardm?qq={e.user_id}", "") ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessageAt(e.user_id, e.group_id, "材料抽卡", "\r\n" + msg);
                    }
                    return quick_reply;
                }
                
                if (e.detail == "材料十连抽卡")
                {
                    List<string> msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/drawcardsm?qq={e.user_id}", "") ?? [];
                    if (msgs.Count > 0)
                    {
                        await Bot.SendGroupMessageAt(e.user_id, e.group_id, "材料十连抽卡", "\r\n" + string.Join("\r\n", msgs));
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 4 && (e.detail.StartsWith("查看库存") || e.detail.StartsWith("我的库存") || e.detail.StartsWith("我的背包")))
                {
                    string detail = e.detail.Replace("查看库存", "").Replace("我的库存", "").Trim();
                    List<string> msgs = [];
                    if (int.TryParse(detail, out int page))
                    {
                        msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo2?qq={e.user_id}&page={page}", "") ?? [];
                    }
                    else if (FunGameItemType.FirstOrDefault(detail.Contains) is string matchedType)
                    {
                        int typeIndex = FunGameItemType.IndexOf(matchedType);
                        string remain = detail.Replace(matchedType, "").Trim();
                        if (int.TryParse(remain, out page))
                        {
                            msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo4?qq={e.user_id}&page={page}&type={typeIndex}", "") ?? [];
                        }
                        else
                        {
                            msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo4?qq={e.user_id}&page=1&type={typeIndex}", "") ?? [];
                        }
                    }
                    else
                    {
                        msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo2?qq={e.user_id}&page=1", "") ?? [];
                    }
                    if (msgs.Count > 0)
                    {
                        await Bot.SendGroupMessageAt(e.user_id, e.group_id, "查看库存", "\r\n" + string.Join("\r\n", msgs));
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 4 && e.detail.StartsWith("物品库存"))
                {
                    string detail = e.detail.Replace("物品库存", "").Trim();
                    List<string> msgs = [];
                    if (int.TryParse(detail, out int page))
                    {
                        msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo3?qq={e.user_id}&page={page}&order=2&orderqty=2", "") ?? [];
                    }
                    else
                    {
                        msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo3?qq={e.user_id}&page=1&order=2&orderqty=2", "") ?? [];
                    }
                    if (msgs.Count > 0)
                    {
                        await Bot.SendGroupMessageAt(e.user_id, e.group_id, "查看分类库存", "\r\n" + string.Join("\r\n", msgs));
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 4 && e.detail.StartsWith("角色库存"))
                {
                    string detail = e.detail.Replace("角色库存", "").Trim();
                    List<string> msgs = [];
                    if (int.TryParse(detail, out int page))
                    {
                        msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo5?qq={e.user_id}&page={page}", "") ?? [];
                    }
                    else
                    {
                        msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo5?qq={e.user_id}&page=1", "") ?? [];
                    }
                    if (msgs.Count > 0)
                    {
                        await Bot.SendGroupMessageAt(e.user_id, e.group_id, "查看角色库存", "\r\n" + string.Join("\r\n", msgs));
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 4 && e.detail.StartsWith("分类库存"))
                {
                    string detail = e.detail.Replace("分类库存", "").Trim();
                    string[] strings = detail.Split(" ");
                    int t = -1;
                    if (strings.Length > 0 && int.TryParse(strings[0].Trim(), out t))
                    {
                        List<string> msgs = [];
                        if (strings.Length > 1 && int.TryParse(strings[1].Trim(), out int page))
                        {
                            msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo4?qq={e.user_id}&page={page}&type={t}", "") ?? [];
                        }
                        else
                        {
                            msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo4?qq={e.user_id}&page=1&type={t}", "") ?? [];
                        }
                        if (msgs.Count > 0)
                        {
                            await Bot.SendGroupMessageAt(e.user_id, e.group_id, "查看分类库存", "\r\n" + string.Join("\r\n", msgs));
                        }
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 3 && e.detail[..3].Equals("我角色", StringComparison.CurrentCultureIgnoreCase))
                {
                    string detail = e.detail.Replace("我角色", "").Trim();
                    string msg = "";
                    if (int.TryParse(detail, out int seq))
                    {
                        msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showcharacterinfo?qq={e.user_id}&seq={seq}&simple=true") ?? "").Trim();
                    }
                    else
                    {
                        msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showcharacterinfo?qq={e.user_id}&seq=1&simple=true") ?? "").Trim();
                    }
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "查库存角色", msg);
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 4 && e.detail[..4].Equals("我的角色", StringComparison.CurrentCultureIgnoreCase))
                {
                    string detail = e.detail.Replace("我的角色", "").Trim();
                    string msg = "";
                    if (int.TryParse(detail, out int seq))
                    {
                        msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showcharacterinfo?qq={e.user_id}&seq={seq}&simple=false") ?? "").Trim();
                    }
                    else
                    {
                        msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showcharacterinfo?qq={e.user_id}&seq=1&simple=false") ?? "").Trim();
                    }
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "查库存角色", msg);
                    }
                    return quick_reply;
                }

                if (e.detail.Length >= 4 && e.detail[..4].Equals("我的物品", StringComparison.CurrentCultureIgnoreCase))
                {
                    string detail = e.detail.Replace("我的物品", "").Trim();
                    if (int.TryParse(detail, out int index))
                    {
                        string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showiteminfo?qq={e.user_id}&seq={index}") ?? "").Trim();
                        if (msg != "")
                        {
                            await Bot.SendGroupMessage(e.group_id, "查库存物品", msg);
                        }
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 4 && e.detail[..4].Equals("兑换金币", StringComparison.CurrentCultureIgnoreCase))
                {
                    string detail = e.detail.Replace("兑换金币", "").Trim();
                    if (int.TryParse(detail, out int materials))
                    {
                        string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/exchangecredits?qq={e.user_id}&materials={materials}") ?? "").Trim();
                        if (msg != "")
                        {
                            await Bot.SendGroupMessage(e.group_id, "兑换金币", msg);
                        }
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 4 && e.detail[..4].Equals("取消装备", StringComparison.CurrentCultureIgnoreCase))
                {
                    string detail = e.detail.Replace("取消装备", "").Trim();
                    string[] strings = detail.Split(" ");
                    int c = -1, i = -1;
                    if (strings.Length > 0 && int.TryParse(strings[0].Trim(), out c) && strings.Length > 1 && int.TryParse(strings[1].Trim(), out i))
                    {
                        if (c != -1 && i != -1)
                        {
                            string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/unequipitem?qq={e.user_id}&c={c}&i={i}") ?? "").Trim();
                            if (msg != "")
                            {
                                await Bot.SendGroupMessage(e.group_id, "取消装备", msg);
                            }
                        }
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 2 && e.detail[..2].Equals("装备", StringComparison.CurrentCultureIgnoreCase))
                {
                    string detail = e.detail.Replace("装备", "").Trim();
                    string[] strings = detail.Split(" ");
                    int c = -1, i = -1;
                    if (strings.Length > 0 && int.TryParse(strings[0].Trim(), out c) && strings.Length > 1 && int.TryParse(strings[1].Trim(), out i))
                    {
                        if (c != -1 && i != -1)
                        {
                            string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/equipitem?qq={e.user_id}&c={c}&i={i}") ?? "").Trim();
                            if (msg != "")
                            {
                                await Bot.SendGroupMessage(e.group_id, "装备", msg);
                            }
                        }
                    }
                    return quick_reply;
                }

                if (e.detail.Length >= 4 && e.detail[..4].Equals("角色升级", StringComparison.CurrentCultureIgnoreCase))
                {
                    string detail = e.detail.Replace("角色升级", "").Trim();
                    string msg = "";
                    if (int.TryParse(detail, out int cid))
                    {
                        msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/characterlevelup?qq={e.user_id}&c={cid}") ?? "").Trim();
                    }
                    else
                    {
                        msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/characterlevelup?qq={e.user_id}&c=1") ?? "").Trim();
                    }
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "角色升级", msg);
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 4 && e.detail[..4].Equals("角色突破", StringComparison.CurrentCultureIgnoreCase))
                {
                    string detail = e.detail.Replace("角色突破", "").Trim();
                    string msg = "";
                    if (int.TryParse(detail, out int cid))
                    {
                        msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/characterlevelbreak?qq={e.user_id}&c={cid}") ?? "").Trim();
                    }
                    else
                    {
                        msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/characterlevelbreak?qq={e.user_id}&c=1") ?? "").Trim();
                    }
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "角色突破", msg);
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 4 && e.detail[..4].Equals("突破信息", StringComparison.CurrentCultureIgnoreCase))
                {
                    string detail = e.detail.Replace("突破信息", "").Trim();
                    string msg = "";
                    if (int.TryParse(detail, out int cid))
                    {
                        msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/getlevelbreakneedy?qq={e.user_id}&id={cid}") ?? "").Trim();
                    }
                    else
                    {
                        msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/getlevelbreakneedy?qq={e.user_id}&id=1") ?? "").Trim();
                    }
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "突破信息", msg);
                    }
                    return quick_reply;
                }

                if (e.detail.Length >= 2 && e.detail[..2].Equals("使用", StringComparison.CurrentCultureIgnoreCase))
                {
                    string detail = e.detail.Replace("使用", "").Trim();
                    char[] chars = [',', ' '];
                    string pattern = @"\s*(?<itemName>[^\d]+)\s*(?<count>\d+)\s*(?:角色\s*(?<characterIds>[\d\s]*))?";
                    Match match = Regex.Match(detail, pattern);
                    if (match.Success)
                    {
                        string itemName = match.Groups["itemName"].Value.Trim();
                        if (int.TryParse(match.Groups["count"].Value, out int count))
                        {
                            string characterIdsString = match.Groups["characterIds"].Value;
                            int[] characterIds = characterIdsString != "" ? [.. characterIdsString.Split(chars, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)] : [1];
                            string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/useitem2?qq={e.user_id}&name={itemName}&count={count}", System.Text.Json.JsonSerializer.Serialize(characterIds)) ?? "").Trim();
                            if (msg != "")
                            {
                                await Bot.SendGroupMessage(e.group_id, "使用", msg);
                            }
                        }
                    }
                    else
                    {
                        pattern = @"\s*(?<itemId>\d+)\s*(?:角色\s*(?<characterIds>[\d\s]*))?";
                        match = Regex.Match(detail, pattern);
                        if (match.Success)
                        {
                            if (int.TryParse(match.Groups["itemId"].Value, out int itemId))
                            {
                                string characterIdsString = match.Groups["characterIds"].Value;
                                int[] characterIds = characterIdsString != "" ? [.. characterIdsString.Split(chars, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)] : [1];
                                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/useitem?qq={e.user_id}&id={itemId}", System.Text.Json.JsonSerializer.Serialize(characterIds)) ?? "").Trim();
                                if (msg != "")
                                {
                                    await Bot.SendGroupMessage(e.group_id, "使用", msg);
                                }
                            }
                        }
                        else
                        {
                            pattern = @"\s*(?<itemName>[^\d]+)\s*(?<count>\d+)\s*";
                            match = Regex.Match(detail, pattern);
                            if (match.Success)
                            {
                                string itemName = match.Groups["itemName"].Value.Trim();
                                if (int.TryParse(match.Groups["count"].Value, out int count))
                                {
                                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/useitem2?qq={e.user_id}&name={itemName}&count={count}") ?? "").Trim();
                                    if (msg != "")
                                    {
                                        await Bot.SendGroupMessage(e.group_id, "使用", msg);
                                    }
                                }
                            }
                            else
                            {
                                pattern = @"\s*(?<itemId>\d+)\s*";
                                match = Regex.Match(detail, pattern);
                                if (match.Success)
                                {
                                    if (int.TryParse(match.Groups["itemId"].Value, out int itemId))
                                    {
                                        string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/useitem?qq={e.user_id}&id={itemId}") ?? "").Trim();
                                        if (msg != "")
                                        {
                                            await Bot.SendGroupMessage(e.group_id, "使用", msg);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return quick_reply;
                }
                
                if (e.detail.Length >= 4 && e.detail[..4].Equals("分解物品", StringComparison.CurrentCultureIgnoreCase))
                {
                    string detail = e.detail.Replace("分解物品", "").Trim();
                    List<int> ids = [];
                    foreach (string str in detail.Split(' '))
                    {
                        if (int.TryParse(str, out int id))
                        {
                            ids.Add(id);
                        }
                    }
                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/decomposeitem?qq={e.user_id}", System.Text.Json.JsonSerializer.Serialize(ids)) ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "分解物品", msg);
                    }

                    return quick_reply;
                }
                
                if (e.detail.Length >= 2 && e.detail[..2].Equals("分解", StringComparison.CurrentCultureIgnoreCase))
                {
                    string detail = e.detail.Replace("分解", "").Trim();
                    string pattern = @"\s*(?<itemName>[^\d]+)\s*(?<count>\d+)\s*";
                    Match match = Regex.Match(detail, pattern);
                    if (match.Success)
                    {
                        string itemName = match.Groups["itemName"].Value.Trim();
                        if (int.TryParse(match.Groups["count"].Value, out int count))
                        {
                            string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/decomposeitem2?qq={e.user_id}&name={itemName}&count={count}") ?? "").Trim();
                            if (msg != "")
                            {
                                await Bot.SendGroupMessage(e.group_id, "分解", msg);
                            }
                        }
                    }

                    return quick_reply;
                }
                
                if (e.detail.Length >= 4 && e.detail[..4].Equals("品质分解", StringComparison.CurrentCultureIgnoreCase))
                {
                    string detail = e.detail.Replace("品质分解", "").Trim();
                    if (int.TryParse(detail, out int q))
                    {
                        string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/decomposeitem3?qq={e.user_id}&q={q}") ?? "").Trim();
                        if (msg != "")
                        {
                            await Bot.SendGroupMessage(e.group_id, "品质分解", msg);
                        }
                    }

                    return quick_reply;
                }

                if (e.detail.Length >= 4 && e.detail[..4].Equals("熟圣之力", StringComparison.CurrentCultureIgnoreCase))
                {
                    string detail = e.detail.Replace("熟圣之力", "").Trim();
                    string[] strings = detail.Split(" ");
                    int count = -1;
                    if (strings.Length > 1 && int.TryParse(strings[1].Trim(), out count))
                    {
                        string name = strings[0].Trim();
                        if (count > 0)
                        {
                            long userid = e.user_id;
                            if (strings.Length > 2 && long.TryParse(strings[2].Replace("@", "").Trim(), out long temp))
                            {
                                userid = temp;
                            }
                            string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/createitem?qq={e.user_id}&name={name}&count={count}&target={userid}") ?? "").Trim();
                            if (msg != "")
                            {
                                await Bot.SendGroupMessage(e.group_id, "熟圣之力", msg);
                            }
                        }
                        else
                        {
                            await Bot.SendGroupMessage(e.group_id, "熟圣之力", "数量不能为0，请重新输入。");
                        }
                    }
                    return quick_reply;
                }

                if (e.detail.Length >= 4 && e.detail[..4].Equals("完整决斗", StringComparison.CurrentCultureIgnoreCase))
                {
                    string detail = e.detail.Replace("完整决斗", "").Replace("@", "").Trim();
                    List<string> msgs = [];
                    if (long.TryParse(detail.Trim(), out long eqq))
                    {
                        msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/fightcustom?qq={e.user_id}&eqq={eqq}&all=true") ?? [];
                    }
                    else
                    {
                        msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/fightcustom2?qq={e.user_id}&name={detail.Trim()}&all=true") ?? [];
                    }
                    List<string> real = [];
                    if (msgs.Count > 2)
                    {
                        if (msgs.Count < 20)
                        {
                            int remain = 7;
                            string merge = "";
                            for (int i = 0; i < msgs.Count - 1; i++)
                            {
                                remain--;
                                merge += msgs[i] + "\r\n";
                                if (remain == 0)
                                {
                                    real.Add(merge);
                                    merge = "";
                                    if ((msgs.Count - i - 2) < 7)
                                    {
                                        remain = msgs.Count - i - 2;
                                    }
                                    else remain = 7;
                                }
                            }
                        }
                        else
                        {
                            real.Add(msgs[^2]);
                        }
                        real.Add(msgs[^1]);
                    }
                    else
                    {
                        real = msgs;
                    }
                    foreach (string msg in real)
                    {
                        await Bot.SendGroupMessage(e.group_id, "完整决斗", msg.Trim());
                        await Task.Delay(1500);
                    }
                    return quick_reply;
                }
                
                if (e.detail.Length >= 2 && e.detail[..2].Equals("决斗", StringComparison.CurrentCultureIgnoreCase))
                {
                    string detail = e.detail.Replace("决斗", "").Replace("@", "").Trim();
                    List<string> msgs = [];
                    if (long.TryParse(detail.Trim(), out long eqq))
                    {
                        msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/fightcustom?qq={e.user_id}&eqq={eqq}&all=false") ?? [];
                    }
                    else
                    {
                        msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/fightcustom2?qq={e.user_id}&name={detail.Trim()}&all=false") ?? [];
                    }
                    List<string> real = [];
                    if (msgs.Count > 2)
                    {
                        int remain = 7;
                        string merge = "";
                        for (int i = 0; i < msgs.Count - 1; i++)
                        {
                            remain--;
                            merge += msgs[i] + "\r\n";
                            if (remain == 0)
                            {
                                real.Add(merge);
                                merge = "";
                                if ((msgs.Count - i - 3) < 7)
                                {
                                    remain = msgs.Count - i - 3;
                                }
                                else remain = 7;
                            }
                        }
                        real.Add(msgs[^1]);
                    }
                    else
                    {
                        real = msgs;
                    }
                    foreach (string msg in real)
                    {
                        await Bot.SendGroupMessage(e.group_id, "决斗", msg.Trim());
                        await Task.Delay(1500);
                    }
                    return quick_reply;
                }

                if (e.user_id == GeneralSettings.Master && e.detail.Length >= 9 && e.detail[..9].Equals("重载FunGame", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return quick_reply;
                    string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/fungame/reload?master=" + GeneralSettings.Master) ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "重载FunGame", msg);
                    }
                    return quick_reply;
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
                        content.message.Add(new TextMessage(string.Concat(name.AsSpan(pos, name.Length > 1 ? 2 : name.Length), "哥")));
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
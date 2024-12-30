using System.Text.RegularExpressions;
using Milimoe.OneBot.Model.Event;
using Milimoe.OneBot.Model.QuickReply;

namespace Milimoe.RainBOT.Settings
{
    public class FunGame
    {
        public static bool FunGameSimulation { get; set; } = false;
        private readonly static List<string> FunGameItemType = ["卡包", "武器", "防具", "鞋子", "饰品", "消耗品", "魔法卡", "收藏品", "特殊物品", "任务物品", "礼包", "其他"];

        public static async Task<bool> Handler(GroupMessageEvent e)
        {
            bool result = true;

            if (e.detail == "帮助")
            {
                await Bot.SendGroupMessage(e.group_id, "饭给木", @"《饭给木》游戏指令列表（第 1 / 3 页）
1、创建存档：创建存档，生成随机一个自建角色（序号固定为1）
2、我的库存/我的背包/查看库存 [页码]：显示所有角色、物品库存，每个角色和物品都有一个专属序号
3、我的库存 <物品类型> [页码]：卡包/武器/防具/鞋子/饰品/消耗品/魔法卡等...
4、分类库存 <物品索引> [页码]：物品索引从0开始，同上...
5、物品库存 [页码]：显示所有角色
* 上述三指令会将物品按品质倒序和数量倒序排序，整合物品序号和数量显示物品库存
6、角色库存 [页码]：显示所有角色
7、我角色 [角色序号]：查看指定序号角色的简略信息，缺省为1
8、我的角色 [角色序号]：查看指定序号角色的详细信息，缺省为1
9、角色重随：重新随机自建角色的属性，需要花材料
10、我的物品 <物品序号>：查看指定序号物品的详细信息
11、设置主战 <角色序号>：将指定序号角色设置为主战
发送【帮助2】查看第 2 页");
            }

            if (e.detail == "帮助2")
            {
                await Bot.SendGroupMessage(e.group_id, "饭给木", @"《饭给木》游戏指令列表（第 2 / 3 页）
12、装备 <角色序号> <物品序号>：装备指定物品给指定角色
13、取消装备 <角色序号> <装备槽序号>：卸下角色指定装备槽上的物品
* 装备槽序号从1开始，卡包/武器/防具/鞋子/饰品1/饰品2
14、角色改名：修改名字，需要金币
15、抽卡/十连抽卡：2000金币一次，还有材料抽卡/材料十连抽卡，10材料1次
16、开启练级 [角色序号]：让指定角色启动练级模式，缺省为1
17、练级结算：收取奖励，最多累计24小时的收益
18、练级信息：查看当前进度
19、角色升级 [角色序号]：升到不能升为止
20、角色突破 [角色序号]：每10/20/30/40/50/60级都要突破才可以继续升级
21、突破信息 [角色序号]：查看下一次突破信息
发送【帮助3】查看第 3 页");
            }

            if (e.detail == "帮助3")
            {
                await Bot.SendGroupMessage(e.group_id, "饭给木", @"《饭给木》游戏指令列表（第 3 / 3 页）
22、普攻升级 [角色序号]：升级普攻等级
23、查看普攻升级 [角色序号]：查看下一次普攻升级信息
23、技能升级 <角色序号> <技能名称>：升级技能等级
24、查看技能升级 <角色序号> <技能名称>：查看下一次技能升级信息
25、使用 <物品名称> <数量> [角色] [角色序号]
26、使用 <物品序号> [角色] [角色序号]
27、使用魔法卡 <魔法卡序号> <魔法卡包序号>
28、合成魔法卡 <{物品序号...}>：要3张魔法卡，空格隔开
29、分解物品 <{物品序号...}>
30、分解 <物品名称> <数量>
31、品质分解 <品质索引>：从0开始，普通/优秀/稀有/史诗/传说/神话/不朽
32、决斗/完整决斗 <@对方>/<QQ号>/<昵称>：和对方切磋
33、兑换金币 <材料数>：1材料=200金币
34、还原存档：没有后悔药");
            }

            if (e.detail.Length >= 9 && e.detail[..9].Equals("FunGame模拟", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return result;
                if (!FunGameSimulation)
                {
                    FunGameSimulation = true;
                    List<string> msgs = await Bot.HttpGet<List<string>>("https://api.milimoe.com/fungame/test?isweb=false", fungame: true) ?? [];
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
                    FunGameSimulation = false;
                }
                else
                {
                    await Bot.SendGroupMessage(e.group_id, "FunGame模拟", "游戏正在模拟中，请勿重复请求！");
                }
                return result;
            }

            if (e.detail.Length >= 11 && e.detail[..11].Equals("FunGame团队模拟", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return result;
                if (!FunGameSimulation)
                {
                    FunGameSimulation = true;
                    List<string> msgs = await Bot.HttpGet<List<string>>("https://api.milimoe.com/fungame/test?isweb=false&isteam=true", fungame: true) ?? [];
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
                    FunGameSimulation = false;
                }
                else
                {
                    await Bot.SendGroupMessage(e.group_id, "FunGame团队模拟", "游戏正在模拟中，请勿重复请求！");
                }
                return result;
            }

            if (e.detail.Length >= 3 && e.detail[..3].Equals("查数据", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return result;
                string detail = e.detail.Replace("查数据", "").Trim();
                if (int.TryParse(detail, out int id))
                {
                    string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/fungame/stats?id=" + id, fungame: true) ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "查询FunGame数据", msg);
                    }
                }
                return result;
            }

            if (e.detail.Length >= 5 && e.detail[..5].Equals("查团队数据", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return result;
                string detail = e.detail.Replace("查团队数据", "").Trim();
                if (int.TryParse(detail, out int id))
                {
                    string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/fungame/teamstats?id=" + id, fungame: true) ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "查询FunGame数据", msg);
                    }
                }
                return result;
            }

            if (e.detail.Length >= 5 && e.detail[..5].Equals("查个人胜率", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return result;
                string[] msg = await Bot.HttpGet<string[]>("https://api.milimoe.com/fungame/winraterank?isteam=false", fungame: true) ?? [];
                if (msg.Length > 0)
                {
                    await Bot.SendGroupMessage(e.group_id, "查询FunGame数据", string.Join("\r\n\r\n", msg));
                }
                return result;
            }

            if (e.detail.Length >= 5 && e.detail[..5].Equals("查团队胜率", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return result;
                string[] msg = await Bot.HttpGet<string[]>("https://api.milimoe.com/fungame/winraterank?isteam=true", fungame: true) ?? [];
                if (msg.Length > 0)
                {
                    await Bot.SendGroupMessage(e.group_id, "查询FunGame数据", string.Join("\r\n\r\n", msg));
                }
                return result;
            }

            if (e.detail.Length >= 3 && e.detail[..3].Equals("查角色", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return result;
                string detail = e.detail.Replace("查角色", "").Trim();
                if (int.TryParse(detail, out int id))
                {
                    string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/fungame/characterinfo?id=" + id, fungame: true) ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "查询FunGame角色技能", msg);
                    }
                }
                return result;
            }

            if (e.detail.Length >= 3 && e.detail[..3].Equals("查技能", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return result;
                string detail = e.detail.Replace("查技能", "").Trim();
                if (int.TryParse(detail, out int id))
                {
                    string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/fungame/skillinfo?id=" + id, fungame: true) ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "查询FunGame角色技能", msg);
                    }
                }
                return result;
            }

            if (e.detail.Length >= 3 && e.detail[..3].Equals("查物品", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return result;
                string detail = e.detail.Replace("查物品", "").Trim();
                if (int.TryParse(detail, out int id))
                {
                    string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/fungame/iteminfo?id=" + id, fungame: true) ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "查询FunGame物品信息", msg);
                    }
                }
                return result;
            }

            if (e.detail.Length >= 6 && e.detail[..6] == "生成魔法卡包")
            {
                if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return result;
                string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/fungame/newmagiccardpack", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessage(e.group_id, "生成魔法卡包", msg);
                }
                return result;
            }
            else if (e.detail.Length >= 5 && e.detail[..5] == "生成魔法卡")
            {
                if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return result;
                string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/fungame/newmagiccard", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessage(e.group_id, "生成魔法卡", msg);
                }
                return result;
            }

            if (e.detail == "创建存档")
            {
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/createsaved?qq={e.user_id}", "", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "创建存档", "\r\n" + msg);
                }
                return result;
            }
            
            if (e.detail == "我的存档")
            {
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showsaved?qq={e.user_id}", "", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "我的存档", "\r\n" + msg);
                }
                return result;
            }
            
            if (e.detail == "我的主战")
            {
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showcharacterinfo?qq={e.user_id}&seq=0", "", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "我的主战", "\r\n" + msg);
                }
                return result;
            }
            
            if (e.detail == "我的小队")
            {
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showsquad?qq={e.user_id}", "", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "我的小队", "\r\n" + msg);
                }
                return result;
            }
            
            if (e.detail == "清空小队")
            {
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/clearsquad?qq={e.user_id}", "", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "清空小队", "\r\n" + msg);
                }
                return result;
            }

            if (e.detail == "还原存档")
            {
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/restoresaved?qq={e.user_id}", "", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "还原存档", "\r\n" + msg);
                }
                return result;
            }

            if (e.detail == "生成自建角色")
            {
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/newcustomcharacter?qq={e.user_id}", "", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "抽卡", "\r\n" + msg);
                }
                return result;
            }

            if (e.detail == "角色改名")
            {
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/rename?qq={e.user_id}", "", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "改名", "\r\n" + msg);
                }
                return result;
            }

            if (e.detail == "角色重随")
            {
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/randomcustom?qq={e.user_id}&confirm=false", "", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "角色重随", "\r\n" + msg);
                }
                return result;
            }

            if (e.detail == "确认角色重随")
            {
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/randomcustom?qq={e.user_id}&confirm=true", "", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "角色重随", "\r\n" + msg);
                }
                return result;
            }

            if (e.detail == "取消角色重随")
            {
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/cancelrandomcustom?qq={e.user_id}", "", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "角色重随", "\r\n" + msg);
                }
                return result;
            }

            if (e.detail == "抽卡")
            {
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/drawcard?qq={e.user_id}", "", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "抽卡", "\r\n" + msg);
                }
                return result;
            }

            if (e.detail == "十连抽卡")
            {
                List<string> msgs = (await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/drawcards?qq={e.user_id}", "", fungame: true) ?? []);
                if (msgs.Count > 0)
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "十连抽卡", "\r\n" + string.Join("\r\n", msgs));
                }
                return result;
            }

            if (e.detail == "材料抽卡")
            {
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/drawcardm?qq={e.user_id}", "", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "材料抽卡", "\r\n" + msg);
                }
                return result;
            }

            if (e.detail == "材料十连抽卡")
            {
                List<string> msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/drawcardsm?qq={e.user_id}", "", fungame: true) ?? [];
                if (msgs.Count > 0)
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "材料十连抽卡", "\r\n" + string.Join("\r\n", msgs));
                }
                return result;
            }

            if (e.detail.Length >= 4 && (e.detail.StartsWith("查看库存") || e.detail.StartsWith("我的库存") || e.detail.StartsWith("我的背包")))
            {
                string detail = e.detail.Replace("查看库存", "").Replace("我的库存", "").Trim();
                List<string> msgs = [];
                if (int.TryParse(detail, out int page))
                {
                    msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo2?qq={e.user_id}&page={page}", "", fungame: true) ?? [];
                }
                else if (FunGameItemType.FirstOrDefault(detail.Contains) is string matchedType)
                {
                    int typeIndex = FunGameItemType.IndexOf(matchedType);
                    string remain = detail.Replace(matchedType, "").Trim();
                    if (int.TryParse(remain, out page))
                    {
                        msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo4?qq={e.user_id}&page={page}&type={typeIndex}", "", fungame: true) ?? [];
                    }
                    else
                    {
                        msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo4?qq={e.user_id}&page=1&type={typeIndex}", "", fungame: true) ?? [];
                    }
                }
                else
                {
                    msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo2?qq={e.user_id}&page=1", "", fungame: true) ?? [];
                }
                if (msgs.Count > 0)
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "查看库存", "\r\n" + string.Join("\r\n", msgs));
                }
                return result;
            }

            if (e.detail.Length >= 4 && e.detail.StartsWith("物品库存"))
            {
                string detail = e.detail.Replace("物品库存", "").Trim();
                List<string> msgs = [];
                if (int.TryParse(detail, out int page))
                {
                    msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo3?qq={e.user_id}&page={page}&order=2&orderqty=2", "", fungame: true) ?? [];
                }
                else
                {
                    msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo3?qq={e.user_id}&page=1&order=2&orderqty=2", "", fungame: true) ?? [];
                }
                if (msgs.Count > 0)
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "查看分类库存", "\r\n" + string.Join("\r\n", msgs));
                }
                return result;
            }

            if (e.detail.Length >= 4 && e.detail.StartsWith("角色库存"))
            {
                string detail = e.detail.Replace("角色库存", "").Trim();
                List<string> msgs = [];
                if (int.TryParse(detail, out int page))
                {
                    msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo5?qq={e.user_id}&page={page}", "", fungame: true) ?? [];
                }
                else
                {
                    msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo5?qq={e.user_id}&page=1", "", fungame: true) ?? [];
                }
                if (msgs.Count > 0)
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "查看角色库存", "\r\n" + string.Join("\r\n", msgs));
                }
                return result;
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
                        msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo4?qq={e.user_id}&page={page}&type={t}", "", fungame: true) ?? [];
                    }
                    else
                    {
                        msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/inventoryinfo4?qq={e.user_id}&page=1&type={t}", "", fungame: true) ?? [];
                    }
                    if (msgs.Count > 0)
                    {
                        await Bot.SendGroupMessageAt(e.user_id, e.group_id, "查看分类库存", "\r\n" + string.Join("\r\n", msgs));
                    }
                }
                return result;
            }

            if (e.detail.Length >= 3 && e.detail[..3].Equals("我角色", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("我角色", "").Trim();
                string msg = "";
                if (int.TryParse(detail, out int seq))
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showcharacterinfo?qq={e.user_id}&seq={seq}&simple=true", fungame: true) ?? "").Trim();
                }
                else
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showcharacterinfo?qq={e.user_id}&seq=1&simple=true", fungame: true) ?? "").Trim();
                }
                if (msg != "")
                {
                    await Bot.SendGroupMessage(e.group_id, "查库存角色", msg);
                }
                return result;
            }

            if (e.detail.Length >= 4 && e.detail[..4].Equals("我的角色", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("我的角色", "").Trim();
                string msg = "";
                if (int.TryParse(detail, out int seq))
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showcharacterinfo?qq={e.user_id}&seq={seq}&simple=false", fungame: true) ?? "").Trim();
                }
                else
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showcharacterinfo?qq={e.user_id}&seq=1&simple=false", fungame: true) ?? "").Trim();
                }
                if (msg != "")
                {
                    await Bot.SendGroupMessage(e.group_id, "查库存角色", msg);
                }
                return result;
            }
            
            if (e.detail.Length >= 4 && e.detail[..4].Equals("角色技能", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("角色技能", "").Trim();
                string msg = "";
                if (int.TryParse(detail, out int seq))
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showcharacterskills?qq={e.user_id}&seq={seq}", fungame: true) ?? "").Trim();
                }
                else
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showcharacterskills?qq={e.user_id}&seq=1", fungame: true) ?? "").Trim();
                }
                if (msg != "")
                {
                    await Bot.SendGroupMessage(e.group_id, "角色技能", msg);
                }
                return result;
            }
            
            if (e.detail.Length >= 4 && e.detail[..4].Equals("角色物品", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("角色物品", "").Trim();
                string msg = "";
                if (int.TryParse(detail, out int seq))
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showcharacteritems?qq={e.user_id}&seq={seq}", fungame: true) ?? "").Trim();
                }
                else
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showcharacteritems?qq={e.user_id}&seq=1", fungame: true) ?? "").Trim();
                }
                if (msg != "")
                {
                    await Bot.SendGroupMessage(e.group_id, "角色物品", msg);
                }
                return result;
            }

            if (e.detail.Length >= 4 && e.detail[..4].Equals("设置主战", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("设置主战", "").Trim();
                string msg = "";
                if (int.TryParse(detail, out int cid))
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/setmain?qq={e.user_id}&c={cid}", fungame: true) ?? "").Trim();
                }
                else
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/setmain?qq={e.user_id}&c=1", fungame: true) ?? "").Trim();
                }
                if (msg != "")
                {
                    await Bot.SendGroupMessage(e.group_id, "设置主战角色", msg);
                }
                return result;
            }

            if (e.detail.Length >= 4 && e.detail[..4].Equals("开启练级", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("开启练级", "").Trim();
                string msg = "";
                if (int.TryParse(detail, out int cid))
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/starttraining?qq={e.user_id}&c={cid}", fungame: true) ?? "").Trim();
                }
                else
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/starttraining?qq={e.user_id}&c=1", fungame: true) ?? "").Trim();
                }
                if (msg != "")
                {
                    await Bot.SendGroupMessage(e.group_id, "开启练级", msg);
                }
                return result;
            }

            if (e.detail == "练级信息")
            {
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/gettraininginfo?qq={e.user_id}", "", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "练级信息", "\r\n" + msg);
                }
                return result;
            }

            if (e.detail == "练级结算")
            {
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/stoptraining?qq={e.user_id}", "", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "练级结算", "\r\n" + msg);
                }
                return result;
            }

            if (e.detail == "材料抽卡")
            {
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/drawcardm?qq={e.user_id}", "", fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessageAt(e.user_id, e.group_id, "材料抽卡", "\r\n" + msg);
                }
                return result;
            }

            if (e.detail.Length >= 4 && e.detail[..4].Equals("我的物品", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("我的物品", "").Trim();
                if (int.TryParse(detail, out int index))
                {
                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/showiteminfo?qq={e.user_id}&seq={index}", fungame: true) ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "查库存物品", msg);
                    }
                }
                return result;
            }

            if (e.detail.Length >= 4 && e.detail[..4].Equals("兑换金币", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("兑换金币", "").Trim();
                if (int.TryParse(detail, out int materials))
                {
                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/exchangecredits?qq={e.user_id}&materials={materials}", fungame: true) ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "兑换金币", msg);
                    }
                }
                return result;
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
                        string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/unequipitem?qq={e.user_id}&c={c}&i={i}", fungame: true) ?? "").Trim();
                        if (msg != "")
                        {
                            await Bot.SendGroupMessage(e.group_id, "取消装备", msg);
                        }
                    }
                }
                return result;
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
                        string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/equipitem?qq={e.user_id}&c={c}&i={i}", fungame: true) ?? "").Trim();
                        if (msg != "")
                        {
                            await Bot.SendGroupMessage(e.group_id, "装备", msg);
                        }
                    }
                }
                return result;
            }

            if (e.detail.Length >= 6 && e.detail[..6].Equals("查看技能升级", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("查看技能升级", "").Trim();
                string[] strings = detail.Split(" ");
                int c = -1;
                if (strings.Length > 0 && int.TryParse(strings[0].Trim(), out c) && strings.Length > 1)
                {
                    string s = strings[1].Trim();
                    if (c != -1 && s != "")
                    {
                        string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/getskilllevelupneedy?qq={e.user_id}&c={c}&s={s}", fungame: true) ?? "").Trim();
                        if (msg != "")
                        {
                            await Bot.SendGroupMessage(e.group_id, "查看技能升级", msg);
                        }
                    }
                }
                return result;
            }

            if (e.detail.Length >= 4 && e.detail[..4].Equals("技能升级", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("技能升级", "").Trim();
                string[] strings = detail.Split(" ");
                int c = -1;
                if (strings.Length > 0 && int.TryParse(strings[0].Trim(), out c) && strings.Length > 1)
                {
                    string s = strings[1].Trim();
                    if (c != -1 && s != "")
                    {
                        string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/skilllevelup?qq={e.user_id}&c={c}&s={s}", fungame: true) ?? "").Trim();
                        if (msg != "")
                        {
                            await Bot.SendGroupMessage(e.group_id, "技能升级", msg);
                        }
                    }
                }
                return result;
            }

            if (e.detail.Length >= 5 && e.detail[..5].Equals("合成魔法卡", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("合成魔法卡", "").Trim();
                string[] strings = detail.Split(" ");
                int id1 = -1, id2 = -1, id3 = -1;
                if (strings.Length > 0 && int.TryParse(strings[0].Trim(), out id1) && strings.Length > 1 && int.TryParse(strings[1].Trim(), out id2) && strings.Length > 2 && int.TryParse(strings[2].Trim(), out id3))
                {
                    if (id1 != -1 && id2 != -1 && id3 != -1)
                    {
                        string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/conflatemagiccardpack?qq={e.user_id}", System.Text.Json.JsonSerializer.Serialize<int[]>([id1, id2, id3]), fungame: true) ?? "").Trim();
                        if (msg != "")
                        {
                            await Bot.SendGroupMessage(e.group_id, "合成魔法卡", msg);
                        }
                    }
                }
                return result;
            }

            if (e.detail.Length >= 4 && e.detail[..4].Equals("角色升级", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("角色升级", "").Trim();
                string msg = "";
                if (int.TryParse(detail, out int cid))
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/characterlevelup?qq={e.user_id}&c={cid}", fungame: true) ?? "").Trim();
                }
                else
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/characterlevelup?qq={e.user_id}&c=1", fungame: true) ?? "").Trim();
                }
                if (msg != "")
                {
                    await Bot.SendGroupMessage(e.group_id, "角色升级", msg);
                }
                return result;
            }

            if (e.detail.Length >= 6 && e.detail[..6].Equals("查看普攻升级", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("查看普攻升级", "").Trim();
                string msg = "";
                if (int.TryParse(detail, out int cid))
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/getnormalattacklevelupneedy?qq={e.user_id}&c={cid}", fungame: true) ?? "").Trim();
                }
                else
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/getnormalattacklevelupneedy?qq={e.user_id}&c=1", fungame: true) ?? "").Trim();
                }
                if (msg != "")
                {
                    await Bot.SendGroupMessage(e.group_id, "查看普攻升级", msg);
                }
                return result;
            }

            if (e.detail.Length >= 4 && e.detail[..4].Equals("普攻升级", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("普攻升级", "").Trim();
                string msg = "";
                if (int.TryParse(detail, out int cid))
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/normalattacklevelup?qq={e.user_id}&c={cid}", fungame: true) ?? "").Trim();
                }
                else
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/normalattacklevelup?qq={e.user_id}&c=1", fungame: true) ?? "").Trim();
                }
                if (msg != "")
                {
                    await Bot.SendGroupMessage(e.group_id, "普攻升级", msg);
                }
                return result;
            }

            if (e.detail.Length >= 4 && e.detail[..4].Equals("角色突破", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("角色突破", "").Trim();
                string msg = "";
                if (int.TryParse(detail, out int cid))
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/characterlevelbreak?qq={e.user_id}&c={cid}", fungame: true) ?? "").Trim();
                }
                else
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/characterlevelbreak?qq={e.user_id}&c=1", fungame: true) ?? "").Trim();
                }
                if (msg != "")
                {
                    await Bot.SendGroupMessage(e.group_id, "角色突破", msg);
                }
                return result;
            }

            if (e.detail.Length >= 4 && e.detail[..4].Equals("突破信息", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("突破信息", "").Trim();
                string msg = "";
                if (int.TryParse(detail, out int cid))
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/getlevelbreakneedy?qq={e.user_id}&id={cid}", fungame: true) ?? "").Trim();
                }
                else
                {
                    msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/getlevelbreakneedy?qq={e.user_id}&id=1", fungame: true) ?? "").Trim();
                }
                if (msg != "")
                {
                    await Bot.SendGroupMessage(e.group_id, "突破信息", msg);
                }
                return result;
            }

            if (e.detail.Length >= 2 && e.detail[..2].Equals("使用", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("使用", "").Trim();
                if (detail.StartsWith("魔法卡"))
                {
                    string pattern = @"\s*魔法卡\s*(?<itemId>\d+)(?:\s*(?:角色\s*)?(?<characterId>\d+))?\s*";
                    Match match = Regex.Match(detail, pattern);
                    if (match.Success)
                    {
                        string itemId = match.Groups["itemId"].Value;
                        string characterId = match.Groups["characterId"].Value;
                        bool isCharacter = detail.Contains("角色");
                        if (int.TryParse(itemId, out int id) && int.TryParse(characterId, out int id2))
                        {
                            if (id > 0 && id2 > 0)
                            {
                                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/useitem3?qq={e.user_id}&id={id}&id2={id2}&c={isCharacter}", fungame: true) ?? "").Trim();
                                if (msg != "")
                                {
                                    await Bot.SendGroupMessage(e.group_id, "使用魔法卡", msg);
                                }
                            }
                        }
                    }
                }
                else
                {
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
                            string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/useitem2?qq={e.user_id}&name={itemName}&count={count}", System.Text.Json.JsonSerializer.Serialize(characterIds), fungame: true) ?? "").Trim();
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
                                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/useitem?qq={e.user_id}&id={itemId}", System.Text.Json.JsonSerializer.Serialize(characterIds), fungame: true) ?? "").Trim();
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
                                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/useitem2?qq={e.user_id}&name={itemName}&count={count}", fungame: true) ?? "").Trim();
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
                                        string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/useitem?qq={e.user_id}&id={itemId}", fungame: true) ?? "").Trim();
                                        if (msg != "")
                                        {
                                            await Bot.SendGroupMessage(e.group_id, "使用", msg);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return result;
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
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/decomposeitem?qq={e.user_id}", System.Text.Json.JsonSerializer.Serialize(ids), fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessage(e.group_id, "分解物品", msg);
                }

                return result;
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
                        string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/decomposeitem2?qq={e.user_id}&name={itemName}&count={count}", fungame: true) ?? "").Trim();
                        if (msg != "")
                        {
                            await Bot.SendGroupMessage(e.group_id, "分解", msg);
                        }
                    }
                }

                return result;
            }

            if (e.detail.Length >= 4 && e.detail[..4].Equals("品质分解", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("品质分解", "").Trim();
                if (int.TryParse(detail, out int q))
                {
                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/decomposeitem3?qq={e.user_id}&q={q}", fungame: true) ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "品质分解", msg);
                    }
                }

                return result;
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
                        string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/createitem?qq={e.user_id}&name={name}&count={count}&target={userid}", fungame: true) ?? "").Trim();
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
                return result;
            }

            if (e.detail.Length >= 4 && e.detail[..4].Equals("完整决斗", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("完整决斗", "").Replace("@", "").Trim();
                List<string> msgs = [];
                if (long.TryParse(detail.Trim(), out long eqq))
                {
                    msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/fightcustom?qq={e.user_id}&eqq={eqq}&all=true", fungame: true) ?? [];
                }
                else
                {
                    msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/fightcustom2?qq={e.user_id}&name={detail.Trim()}&all=true", fungame: true) ?? [];
                }
                List<string> real = [];
                if (msgs.Count >= 2)
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
                return result;
            }

            if (e.detail.Length >= 2 && e.detail[..2].Equals("决斗", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("决斗", "").Replace("@", "").Trim();
                List<string> msgs = [];
                if (long.TryParse(detail.Trim(), out long eqq))
                {
                    msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/fightcustom?qq={e.user_id}&eqq={eqq}&all=false", fungame: true) ?? [];
                }
                else
                {
                    msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/fightcustom2?qq={e.user_id}&name={detail.Trim()}&all=false", fungame: true) ?? [];
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
                return result;
            }
            
            if (e.detail.Length >= 4 && e.detail[..4].Equals("小队决斗", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("小队决斗", "").Replace("@", "").Trim();
                List<string> msgs = [];
                if (long.TryParse(detail.Trim(), out long eqq))
                {
                    msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/fightcustomteam?qq={e.user_id}&eqq={eqq}&all=true", fungame: true) ?? [];
                }
                else
                {
                    msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/fightcustomteam2?qq={e.user_id}&name={detail.Trim()}&all=true", fungame: true) ?? [];
                }
                List<string> real = [];
                if (msgs.Count >= 3)
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
                                if ((msgs.Count - i - 3) < 7)
                                {
                                    remain = msgs.Count - i - 3;
                                }
                                else remain = 7;
                            }
                        }
                    }
                    else
                    {
                        real.Add(msgs[^3]);
                    }
                    real.Add(msgs[^2]);
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
                return result;
            }

            if (e.detail.Length >= 6 && e.detail[..6].Equals("查询boss", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("查询boss", "").Trim();
                List<string> msgs = [];
                if (int.TryParse(detail, out int cid))
                {
                    msgs = await Bot.HttpGet<List<string>>($"https://api.milimoe.com/fungame/getboss?index={cid}", fungame: true) ?? [];
                }
                else
                {
                    msgs = await Bot.HttpGet<List<string>>($"https://api.milimoe.com/fungame/getboss", fungame: true) ?? [];
                }
                if (msgs.Count > 0)
                {
                    await Bot.SendGroupMessage(e.group_id, "BOSS", string.Join("\r\n", msgs));
                }
                return result;
            }
            
            if (e.detail.Length >= 8 && e.detail[..8].Equals("小队讨伐boss", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("小队讨伐boss", "").Trim();
                List<string> msgs = [];
                if (int.TryParse(detail.Trim(), out int index))
                {
                    msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/fightbossteam?qq={e.user_id}&index={index}&all=true", fungame: true) ?? [];
                    List<string> real = [];
                    if (msgs.Count >= 3)
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
                                    if ((msgs.Count - i - 3) < 7)
                                    {
                                        remain = msgs.Count - i - 3;
                                    }
                                    else remain = 7;
                                }
                            }
                        }
                        else
                        {
                            real.Add(msgs[^3]);
                        }
                        real.Add(msgs[^2]);
                        real.Add(msgs[^1]);
                    }
                    else
                    {
                        real = msgs;
                    }
                    foreach (string msg in real)
                    {
                        await Bot.SendGroupMessage(e.group_id, "BOSS", msg.Trim());
                        await Task.Delay(1500);
                    }
                }
                else
                {
                    await Bot.SendGroupMessage(e.group_id, "BOSS", "请输入正确的编号！");
                }
                return result;
            }
            
            if (e.detail.Length >= 6 && e.detail[..6].Equals("讨伐boss", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("讨伐boss", "").Trim();
                List<string> msgs = [];
                if (int.TryParse(detail.Trim(), out int index))
                {
                    msgs = await Bot.HttpPost<List<string>>($"https://api.milimoe.com/fungame/fightboss?qq={e.user_id}&index={index}&all=true", fungame: true) ?? [];
                    List<string> real = [];
                    if (msgs.Count >= 3)
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
                                    if ((msgs.Count - i - 3) < 7)
                                    {
                                        remain = msgs.Count - i - 3;
                                    }
                                    else remain = 7;
                                }
                            }
                        }
                        else
                        {
                            real.Add(msgs[^3]);
                        }
                        real.Add(msgs[^2]);
                        real.Add(msgs[^1]);
                    }
                    else
                    {
                        real = msgs;
                    }
                    foreach (string msg in real)
                    {
                        await Bot.SendGroupMessage(e.group_id, "BOSS", msg.Trim());
                        await Task.Delay(1500);
                    }
                }
                else
                {
                    await Bot.SendGroupMessage(e.group_id, "BOSS", "请输入正确的编号！");
                }
                return result;
            }

            if (e.detail.Length >= 4 && e.detail[..4].Equals("小队添加", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("小队添加", "").Trim();
                if (int.TryParse(detail, out int c))
                {
                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/addsquad?qq={e.user_id}&c={c}", fungame: true) ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "小队", msg);
                    }
                }
                return result;
            }
            
            if (e.detail.Length >= 4 && e.detail[..4].Equals("小队移除", StringComparison.CurrentCultureIgnoreCase))
            {
                string detail = e.detail.Replace("小队移除", "").Trim();
                if (int.TryParse(detail, out int c))
                {
                    string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/removesquad?qq={e.user_id}&c={c}", fungame: true) ?? "").Trim();
                    if (msg != "")
                    {
                        await Bot.SendGroupMessage(e.group_id, "小队", msg);
                    }
                }
                return result;
            }
            
            if (e.detail.Length >= 4 && (e.detail.StartsWith("设置小队") || e.detail.StartsWith("重组小队")))
            {
                string detail = e.detail.Replace("设置小队", "").Replace("重组小队", "").Trim();
                string[] strings = detail.Split(' ');
                List<int> cindexs = [];
                foreach (string s in strings)
                {
                    if (int.TryParse(s, out int c))
                    {
                        cindexs.Add(c);
                    }
                }
                string msg = (await Bot.HttpPost<string>($"https://api.milimoe.com/fungame/setsquad?qq={e.user_id}", System.Text.Json.JsonSerializer.Serialize(cindexs), fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessage(e.group_id, "小队", msg);
                }
                return result;
            }

            if (e.user_id == GeneralSettings.Master && e.detail.Length >= 9 && e.detail[..9].Equals("重载FunGame", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!await Bot.CheckBlackList(true, e.user_id, e.group_id)) return result;
                string msg = (await Bot.HttpGet<string>("https://api.milimoe.com/fungame/reload?master=" + GeneralSettings.Master, fungame: true) ?? "").Trim();
                if (msg != "")
                {
                    await Bot.SendGroupMessage(e.group_id, "重载FunGame", msg);
                }
                return result;
            }

            return false;
        }
    }
}

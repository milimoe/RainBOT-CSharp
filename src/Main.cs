using System.Text.RegularExpressions;
using Milimoe.OneBot.Framework;
using Milimoe.OneBot.Model.Message;
using Milimoe.OneBot.Model.Other;
using Milimoe.OneBot.Model.QuickReply;
using Milimoe.RainBOT.Command;
using Milimoe.RainBOT.Settings;

try
{
    // Debug模式启动项
    if (args.Contains("--debug"))
    {
        GeneralSettings.IsDebug = true;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Debug模式");
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    if (args.Any(a => a.StartsWith("-g")))
    {
        string debug_group = args.Where(a => a.StartsWith("-g")).FirstOrDefault() ?? "";
        debug_group = debug_group.Replace("-g", "").Trim();
        if (long.TryParse(debug_group, out long group_id))
        {
            GeneralSettings.DebugGroupID = group_id;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("已指定Debug调试沙盒群聊：" + GeneralSettings.DebugGroupID);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }

    HTTPListener? listener = default;

    // 首先需要创建一个HTTP监听器
    while (true)
    {
        try
        {
            listener = new();
            if (listener.available)
            {
                break;
            }
        }
        catch (Exception e_create_listener)
        {
            Console.WriteLine(e_create_listener);
            Console.Write("你想继续吗？[y/n]");
            ConsoleKeyInfo c = Console.ReadKey();
            if (c.Key == ConsoleKey.N)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" Stop");
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            }
            else
            {
                Console.WriteLine();
            }
        }
    }

    if (listener is null || !listener.available)
    {
        return;
    }

    Console.WriteLine("初始化参数设定...");
    GeneralSettings.LoadSetting();
    if (GeneralSettings.BotQQ == -1)
    {
        Console.Write("请设定BotQQ（修仙的QQ）：");
        if (long.TryParse(Console.ReadLine(), out long bot_qq))
        {
            GeneralSettings.BotQQ = bot_qq;
        }
    }
    if (GeneralSettings.Master == -1)
    {
        Console.Write("请设定MasterQQ（不方便操作修仙QQ时，可以用另一个QQ控制）：");
        if (long.TryParse(Console.ReadLine(), out long master_qq))
        {
            GeneralSettings.Master = master_qq;
        }
    }
    Console.WriteLine("保存参数设定...");
    GeneralSettings.SaveConfig();

    try
    {
        Console.WriteLine("初始化BotQQ群列表...");
        await Bot.GetGroups();

        Console.WriteLine("初始化BotQQ群成员列表...");
        await Bot.GetGroupMembers();
    }
    catch (Exception e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(e);
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    Console.WriteLine("初始化修仙参数设定...");
    修仙.Init();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("初始化完毕！");
    Console.ForegroundColor = ConsoleColor.Gray;

    Console.WriteLine("开始监听 -> " + listener.address);

    // 绑定监听事件
    listener.FriendMessageListening += new HTTPListener.FriendMessageListeningTask(async (e) =>
    {
        FriendMsgEventQuickReply? quick_reply = null;

        try
        {
            Sender sender = e.sender;

            if (e.user_id == 修仙.小北QQ)
            {
                Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} P/{e.user_id}{(e.detail.Trim() == "" ? "" : " -> " + Regex.Replace(e.detail, @"\r(?!\n)", "\r\n"))}");
                if (GeneralSettings.IsDebug)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"DEBUG：{e.original_msg}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    await Task.Delay(100);
                }

                if (修仙.小北.修仙状态.炼金药材 && e.message.Where(m => m is MarkdownMessage).FirstOrDefault() is MarkdownMessage md)
                {
                    await 修仙.小北.自动炼金药材(md.data.data);
                }

                if (修仙.小北.修仙状态.世界BOSS != "" && e.message.Where(m => m is MarkdownMessage).FirstOrDefault() is MarkdownMessage md1)
                {
                    修仙.小北.打BOSS(md1.data.data);
                }

                if (修仙.小北.修仙状态.悬赏令 && e.message.Where(m => m is MarkdownMessage).FirstOrDefault() is MarkdownMessage md2)
                {
                    修仙.小北.自动悬赏令(md2.data.data);
                }

                if (修仙.小北.修仙状态.秘境 && e.message.Where(m => m is MarkdownMessage).FirstOrDefault() is MarkdownMessage md3)
                {
                    修仙.小北.自动秘境(md3.data.data);
                }

                if (e.detail.Contains("秘境") && e.message.Where(m => m is MarkdownMessage).FirstOrDefault() is MarkdownMessage md4)
                {
                    _ = Bot.SendFriendMessage(GeneralSettings.Master, "秘境", "【小北】" + md4.data.data);
                }

                return quick_reply;
            }

            if (e.user_id == GeneralSettings.Master)
            {
                Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} M/来自主人{(e.detail.Trim() == "" ? "" : " -> " + e.detail)}");
                if (GeneralSettings.IsDebug)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"DEBUG：{e.original_msg}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                switch (e.detail)
                {
                    case "是":
                        await Bot.SendFriendMessage(e.user_id, "随机反驳是", "是你的头");
                        break;
                    case "炼金药材":
                        修仙.小北.修仙状态.炼金药材 = true;
                        if (修仙.开启小小修炼) 修仙.小小.修仙状态.炼金药材 = true;
                        await 修仙.发消息("炼金药材", "药材背包");
                        break;
                    case "暂停":
                        修仙.开启自动修炼 = false;
                        修仙.小北.开启修炼 = false;
                        修仙.小小.开启修炼 = false;
                        await Bot.SendFriendMessage(e.user_id, "暂停修炼", "暂停修炼");
                        break;
                    case "继续":
                        修仙.开启自动修炼 = true;
                        修仙.小北.开启修炼 = true;
                        修仙.小小.开启修炼 = true;
                        await Bot.SendFriendMessage(e.user_id, "继续修炼", "继续修炼");
                        break;
                    case "暂停小北":
                        修仙.小北.开启修炼 = false;
                        await Bot.SendFriendMessage(e.user_id, "暂停小北修炼", "暂停小北修炼");
                        break;
                    case "继续小北":
                        修仙.小北.开启修炼 = true;
                        await Bot.SendFriendMessage(e.user_id, "继续小北修炼", "继续小北修炼");
                        break;
                    case "暂停小小":
                        修仙.小小.开启修炼 = false;
                        await Bot.SendFriendMessage(e.user_id, "暂停小小修炼", "暂停小小修炼");
                        break;
                    case "继续小小":
                        修仙.小小.开启修炼 = true;
                        await Bot.SendFriendMessage(e.user_id, "继续小小修炼", "继续小小修炼");
                        break;
                    case "破5次":
                        _ = Task.Run(async () =>
                        {
                            await Bot.SendFriendMessage(e.user_id, "破5次", "渡厄突破");
                            await Task.Delay(1500);
                        });
                        break;
                    case "闭关":
                        修仙.小北.修仙状态.闭关 = true;
                        if (修仙.开启小小修炼) 修仙.小小.修仙状态.闭关 = true;
                        await 修仙.发消息("闭关", "闭关");
                        await Bot.SendFriendMessage(e.user_id, "闭关", "闭关");
                        break;
                    case "出关":
                        修仙.小北.修仙状态.闭关 = false;
                        if (修仙.开启小小修炼) 修仙.小小.修仙状态.闭关 = false;
                        await 修仙.发消息("出关", "出关");
                        await Bot.SendFriendMessage(e.user_id, "出关", "出关");
                        break;
                }

                // OSM指令
                if (e.detail.Length >= 4 && e.detail[..4] == ".osm")
                {
                    MasterCommand.Execute(e.detail, e.user_id, false, e.user_id, false);
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
    });

    listener.GroupMessageListening += new HTTPListener.GroupMessageListeningTask(async (e) =>
    {
        GroupMsgEventQuickReply quick_reply = new();

        try
        {
            if (修仙.私聊模式 || e.group_id != 修仙.指定群聊) return quick_reply;

            if (e.user_id == 修仙.小北QQ || e.user_id == 修仙.小小QQ)
            {
                修仙控制器 修仙控制器 = e.user_id == 修仙.小北QQ ? 修仙.小北 : 修仙.小小;
                修仙状态 修仙状态 = 修仙控制器.修仙状态;

                Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} G/{e.group_id} U/{e.user_id}{(e.detail.Trim() == "" ? "" : " -> " + Regex.Replace(e.detail, @"\r(?!\n)", "\r\n"))}");
                if (GeneralSettings.IsDebug)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"DEBUG：{e.original_msg}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    await Task.Delay(100);
                }

                if (修仙状态.炼金药材 && e.message.Where(m => m is MarkdownMessage).FirstOrDefault() is MarkdownMessage md)
                {
                    await 修仙控制器.自动炼金药材(md.data.data);
                }

                if (e.user_id == 修仙.小北QQ && 修仙状态.世界BOSS != "" && e.message.Where(m => m is MarkdownMessage).FirstOrDefault() is MarkdownMessage md1)
                {
                    修仙控制器.打BOSS(md1.data.data);
                }

                if (修仙状态.悬赏令 && e.message.Where(m => m is MarkdownMessage).FirstOrDefault() is MarkdownMessage md2)
                {
                    修仙控制器.自动悬赏令(md2.data.data);
                }

                if (修仙状态.秘境 && e.message.Where(m => m is MarkdownMessage).FirstOrDefault() is MarkdownMessage md3)
                {
                    修仙控制器.自动秘境(md3.data.data);
                }

                if (e.detail.Contains("秘境") && e.message.Where(m => m is MarkdownMessage).FirstOrDefault() is MarkdownMessage md4)
                {
                    _ = Bot.SendFriendMessage(GeneralSettings.Master, "秘境", (e.user_id == 修仙.小北QQ ? "【小北】" : "【小小】") + md4.data.data);
                }

                return quick_reply;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        return quick_reply;
    });

    _ = Task.Factory.StartNew(async () =>
    {
        bool 悬赏令控制 = false;
        bool 秘境控制 = false;
        while (true)
        {
            try
            {
                DateTime now = DateTime.Now;
                if (now.Hour == 9 && now.Minute == 0 && 修仙.开启自动悬赏令 && !悬赏令控制)
                {
                    悬赏令控制 = true;
                    修仙.开启自动修炼 = false;
                    修仙.开启自动悬赏令 = false;
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("修炼关闭，准备做悬赏！");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                if (now.Hour == 9 && now.Minute == 3 && 悬赏令控制)
                {
                    悬赏令控制 = false;
                    修仙.小北.修仙状态.悬赏令 = true;
                    if (修仙.开启小小修炼) 修仙.小小.修仙状态.悬赏令 = true;
                    await 修仙.发消息("悬赏令刷新", $"悬赏令刷新");
                }
                if (now.Hour == 12 && now.Minute == 9 && 修仙.开启自动灵田收取宗门丹药领取)
                {
                    修仙.开启自动灵田收取宗门丹药领取 = false;
                    await 修仙.发消息("灵田收取", $"灵田收取");
                    await 修仙.发消息("宗门丹药领取", $"宗门丹药领取");
                }
                if (now.Hour == 12 && now.Minute > 9 && !修仙.开启自动灵田收取宗门丹药领取)
                {
                    修仙.开启自动灵田收取宗门丹药领取 = true;
                }
                if (now.Hour == 15 && now.Minute == 0 && 修仙.开启自动秘境 && !秘境控制)
                {
                    秘境控制 = true;
                    修仙.开启自动修炼 = false;
                    修仙.开启自动秘境 = false;
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("修炼关闭，准备做秘境！");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                if (now.Hour == 15 && now.Minute == 3 && 秘境控制)
                {
                    秘境控制 = false;
                    修仙.小北.修仙状态.秘境 = true;
                    if (修仙.开启小小修炼) 修仙.小小.修仙状态.秘境 = true;
                    await 修仙.发消息("秘境", $"探索秘境");
                }
                await Task.Delay(1000);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    });

    _ = Task.Factory.StartNew(async () =>
    {
        if (修仙.开启自动修炼)
        {
            修仙.小北.开启修炼 = true;
        }
        while (true)
        {
            try
            {
                if (修仙.开启自动修炼 && 修仙.小北.开启修炼 && 修仙.开启自动突破 && 修仙.小北.修仙状态.修炼次数 != -1 && 修仙.小北.修仙状态.修炼次数++ == 修仙.每修炼几次破一次)
                {
                    await 修仙.发消息("自动突破", "渡厄突破", 修仙.小北QQ);
                    修仙.小北.修仙状态.修炼次数 = 0;
                    await Task.Delay(1000);
                }
                if (!修仙.小北.修仙状态.闭关 && !修仙.小北.修仙状态.在做悬赏令 && !修仙.小北.修仙状态.在秘境中 && 修仙.开启自动修炼 && 修仙.小北.开启修炼) await 修仙.发消息("修炼", "修炼", 修仙.小北QQ);
                await Task.Delay(1000 * 60 * 2 + 1000 * 8);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    });
    
    _ = Task.Factory.StartNew(async () =>
    {
        if (修仙.开启小小修炼)
        {
            修仙.小小.开启修炼 = true;
        }
        while (true)
        {
            try
            {
                if (修仙.开启自动修炼 && 修仙.开启自动突破 && 修仙.开启小小修炼 && 修仙.小小.开启修炼 && 修仙.小小.修仙状态.修炼次数 != -1 && 修仙.小小.修仙状态.修炼次数++ == 修仙.每修炼几次破一次)
                {
                    await 修仙.发消息("自动突破", "渡厄突破", 修仙.小小QQ);
                    修仙.小小.修仙状态.修炼次数 = 0;
                    await Task.Delay(1000);
                }
                if (!修仙.小小.修仙状态.闭关 && !修仙.小小.修仙状态.在做悬赏令 && !修仙.小小.修仙状态.在秘境中 && 修仙.开启自动修炼 && 修仙.开启小小修炼 && 修仙.小小.开启修炼) await 修仙.发消息("修炼", "修炼", 修仙.小小QQ);
                await Task.Delay(1000 * 60 + 1000 * 6);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    });

    bool isListening = true;
    CancellationTokenSource cts = new();
    CancellationToken ct = cts.Token;

    Task t = Task.Factory.StartNew(() =>
    {
        // 循环接收消息，此线程会在没有请求时阻塞
        while (isListening)
        {
            try
            {
                listener.GetContext();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }, ct);

    while (true)
    {
        string read = Console.ReadLine() ?? "";
        // OSM指令
        if (read.Length >= 4 && read[..4] == ".osm")
        {
            MasterCommand.Execute(read, GeneralSettings.Master, false, GeneralSettings.Master, false);
            continue;
        }
        if (read.Length >= 4 && read[..4].Equals("boss", StringComparison.CurrentCultureIgnoreCase))
        {
            string str = read.ToLower().Replace("boss", "").Trim();
            修仙.小北.修仙状态.世界BOSS = str;
            await 修仙.发消息("BOSS", "查询世界boss", 修仙.小北QQ);
            continue;
        }
        switch (read.ToLower().Trim() ?? "")
        {
            case "炼金药材":
                修仙.小北.修仙状态.炼金药材 = true;
                if (修仙.开启小小修炼) 修仙.小小.修仙状态.炼金药材 = true;
                await 修仙.发消息("炼金药材", "药材背包");
                break;
            case "暂停":
                Console.ForegroundColor = ConsoleColor.Yellow;
                修仙.开启自动修炼 = false;
                修仙.小北.开启修炼 = false;
                修仙.小小.开启修炼 = false;
                Console.WriteLine("暂停修炼");
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case "继续":
                Console.ForegroundColor = ConsoleColor.Yellow;
                修仙.开启自动修炼 = true;
                修仙.小北.开启修炼 = true;
                修仙.小小.开启修炼 = true;
                Console.WriteLine("继续修炼");
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case "暂停小北":
                Console.ForegroundColor = ConsoleColor.Yellow;
                修仙.小北.开启修炼 = false;
                Console.WriteLine("暂停小北修炼");
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case "继续小北":
                Console.ForegroundColor = ConsoleColor.Yellow;
                修仙.小北.开启修炼 = true;
                Console.WriteLine("继续小北修炼");
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case "暂停小小":
                Console.ForegroundColor = ConsoleColor.Yellow;
                修仙.小小.开启修炼 = false;
                Console.WriteLine("暂停小小修炼");
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case "继续小小":
                Console.ForegroundColor = ConsoleColor.Yellow;
                修仙.小小.开启修炼 = true;
                Console.WriteLine("继续小小修炼");
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case "破5次":
                _ = Task.Run(async () =>
                {
                    await 修仙.发消息("破5次", "渡厄突破");
                    await Task.Delay(1500);
                });
                break;
            case "闭关":
                修仙.小北.修仙状态.闭关 = true;
                if (修仙.开启小小修炼) 修仙.小小.修仙状态.闭关 = true;
                await 修仙.发消息("闭关", "闭关");
                break;
            case "出关":
                修仙.小北.修仙状态.闭关 = false;
                if (修仙.开启小小修炼) 修仙.小小.修仙状态.闭关 = false;
                await 修仙.发消息("出关", "出关");
                break;
            case "debug on":
                GeneralSettings.IsDebug = true;
                Console.WriteLine("开启Debug模式");
                break;
            case "debug off":
                GeneralSettings.IsDebug = false;
                Console.WriteLine("关闭Debug模式");
                break;
        }
    }
}
catch (Exception e)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(e);
    Console.ForegroundColor = ConsoleColor.Gray;
}

Console.ReadKey();
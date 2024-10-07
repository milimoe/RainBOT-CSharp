using Milimoe.OneBot.Framework;
using Milimoe.RainBOT.Command;
using Milimoe.RainBOT.ListeningTask;
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
    listener.FriendMessageListening += FriendMessageTask.ListeningTask_handler;

    _ = Task.Factory.StartNew(async () =>
    {
        while (true)
        {
            try
            {
                DateTime now = DateTime.Now;
                if (now.Hour == 9 && now.Minute == 0 && 修仙.开启自动悬赏令)
                {
                    修仙.开启自动修炼 = false;
                    修仙.开启自动悬赏令 = false;
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("修炼关闭，准备做悬赏！");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                if (now.Hour == 9 && now.Minute == 3)
                {
                    修仙状态.悬赏令 = true;
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
                if (now.Hour == 15 && now.Minute == 0 && 修仙.开启自动秘境)
                {
                    修仙.开启自动修炼 = false;
                    修仙.开启自动秘境 = false;
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("修炼关闭，准备做秘境！");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                if (now.Hour == 15 && now.Minute == 3)
                {
                    修仙状态.秘境 = true;
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
        while (true)
        {
            try
            {
                if (修仙.开启自动突破 && 修仙状态.修炼次数 != -1 && 修仙状态.修炼次数++ == 修仙.每修炼几次破一次)
                {
                    await 修仙.发消息("自动突破", "渡厄突破");
                    修仙状态.修炼次数 = 0;
                    await Task.Delay(1000);
                }
                if (!修仙状态.闭关 && 修仙.开启自动修炼) await 修仙.发消息("修炼", "修炼");
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
            修仙状态.世界BOSS = str;
            await 修仙.发消息("BOSS", "查询世界boss");
            continue;
        }
        switch (read.ToLower().Trim() ?? "")
        {
            case "炼金药材":
                修仙状态.炼金药材 = true;
                await 修仙.发消息("炼金药材", "药材背包");
                break;
            case "暂停":
                Console.ForegroundColor = ConsoleColor.Yellow;
                修仙.开启自动修炼 = false;
                Console.WriteLine("暂停修炼");
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case "继续":
                Console.ForegroundColor = ConsoleColor.Yellow;
                修仙.开启自动修炼 = true;
                Console.WriteLine("继续修炼");
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case "闭关":
                Console.ForegroundColor = ConsoleColor.Yellow;
                修仙状态.闭关 = true;
                await 修仙.发消息("闭关", "闭关");
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case "出关":
                Console.ForegroundColor = ConsoleColor.Yellow;
                修仙状态.闭关 = false;
                await 修仙.发消息("出关", "出关");
                Console.ForegroundColor = ConsoleColor.Gray;
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
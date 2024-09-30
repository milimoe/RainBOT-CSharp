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
        Console.Write("请设定BotQQ，否则某些功能无法正常运行：");
        if (long.TryParse(Console.ReadLine(), out long bot_qq))
        {
            GeneralSettings.BotQQ = bot_qq;
        }
    }
    if (GeneralSettings.Master == -1)
    {
        Console.Write("请设定MasterQQ，否则某些功能无法正常运行：");
        if (long.TryParse(Console.ReadLine(), out long master_qq))
        {
            GeneralSettings.Master = master_qq;
        }
    }
    Console.WriteLine("保存参数设定...");
    GeneralSettings.SaveConfig();

    try
    {
        Console.WriteLine("初始化机器人QQ群列表...");
        await Bot.GetGroups();

        Console.WriteLine("初始化机器人QQ群成员列表...");
        await Bot.GetGroupMembers();
    }
    catch (Exception e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(e);
        Console.ForegroundColor = ConsoleColor.Gray;
    }

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
                if (now.Hour == 9 && now.Minute == 0)
                {
                    FriendMessageTask.修炼 = false;
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("修炼关闭，准备做悬赏！");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                if (now.Hour == 9 && now.Minute == 3)
                {
                    FriendMessageTask.正在悬赏令 = true;
                    await Bot.SendFriendMessage(3889029313, "悬赏令刷新", $"悬赏令刷新");
                }
                if (now.Hour == 12 && now.Minute == 09)
                {
                    await Bot.SendFriendMessage(3889029313, "灵田收取", $"灵田收取");
                }
                if (now.Hour == 15 && now.Minute == 0)
                {
                    FriendMessageTask.修炼 = false;
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("修炼关闭，准备做秘境！");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                if (now.Hour == 15 && now.Minute == 3)
                {
                    FriendMessageTask.秘境 = true;
                    await Bot.SendFriendMessage(3889029313, "秘境", $"探索秘境");
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
                if (FriendMessageTask.修炼次数 != -1 && FriendMessageTask.修炼次数++ == 8)
                {
                    await Bot.SendFriendMessage(3889029313, "自动突破", "渡厄突破");
                    FriendMessageTask.修炼次数 = 0;
                    await Task.Delay(1000);
                }
                if (!FriendMessageTask.闭关 && FriendMessageTask.修炼) await Bot.SendFriendMessage(3889029313, "修炼", "修炼");
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
            FriendMessageTask.世界BOSS = str;
            await Bot.SendFriendMessage(3889029313, "BOSS", "查询世界boss");
            continue;
        }
        if (read.Length >= 5 && read[..5] == "悬赏令接取")
        {
            string str = read.ToLower().Replace("悬赏令接取", "").Trim();
            if (int.TryParse(str, out int value))
            {
                await Bot.SendFriendMessage(3889029313, "悬赏令接取", $"悬赏令接取{value}");
            }
            continue;
        }
        switch (read.ToLower().Trim() ?? "")
        {
            case "炼金药材":
                FriendMessageTask.炼金药材 = true;
                await Bot.SendFriendMessage(3889029313, "炼金药材", "药材背包");
                break;
            case "暂停":
                Console.ForegroundColor = ConsoleColor.Yellow;
                FriendMessageTask.修炼 = false;
                Console.WriteLine("暂停修炼");
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case "继续":
                Console.ForegroundColor = ConsoleColor.Yellow;
                FriendMessageTask.修炼 = true;
                Console.WriteLine("继续修炼");
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case "闭关":
                Console.ForegroundColor = ConsoleColor.Yellow;
                FriendMessageTask.闭关 = true;
                await Bot.SendFriendMessage(3889029313, "闭关", "闭关");
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case "出关":
                Console.ForegroundColor = ConsoleColor.Yellow;
                FriendMessageTask.闭关 = false;
                await Bot.SendFriendMessage(3889029313, "出关", "出关");
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case "探索秘境":
                Console.ForegroundColor = ConsoleColor.Yellow;
                FriendMessageTask.修炼 = false;
                FriendMessageTask.秘境 = true;
                await Bot.SendFriendMessage(3889029313, "探索秘境", "探索秘境");
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case "秘境结算":
                if (FriendMessageTask.秘境)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    FriendMessageTask.修炼 = true;
                    FriendMessageTask.秘境 = false;
                    await Bot.SendFriendMessage(3889029313, "秘境结算", "秘境结算");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                break;
            case "悬赏令刷新":
                Console.ForegroundColor = ConsoleColor.Yellow;
                FriendMessageTask.修炼 = false;
                await Bot.SendFriendMessage(3889029313, "悬赏令刷新", "悬赏令刷新");
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case "悬赏令结算":
                if (FriendMessageTask.正在悬赏令)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    FriendMessageTask.修炼 = true;
                    await Bot.SendFriendMessage(3889029313, "悬赏令结算", "悬赏令结算");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                break;
            case "自动突破":
                FriendMessageTask.修炼次数 = 8;
                Console.WriteLine("开启自动突破");
                break;
            case "不自动突破":
                FriendMessageTask.修炼次数 = -1;
                Console.WriteLine("关闭自动突破");
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
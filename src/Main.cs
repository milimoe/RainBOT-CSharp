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

    Console.WriteLine("初始化音频/运势/词汇列表...");
    Music.InitMusicList();
    Daily.InitDaily();
    SayNo.InitSayNo();
    Ignore.InitIgnore();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("初始化完毕！");
    Console.ForegroundColor = ConsoleColor.Gray;

    Console.WriteLine("开始监听 -> " + listener.address);

    // 绑定监听事件
    listener.GroupMessageListening += GroupMessageTask.ListeningTask_handler;
    //listener.GroupBanNoticeListening += GroupBanTask.ListeningTask_handler;
    listener.FriendMessageListening += FriendMessageTask.ListeningTask_handler;

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
        switch (read.ToLower().Trim() ?? "")
        {
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
namespace Milimoe.RainBOT.Model
{
    public class UserDaily(long user_id, long type, string daily)
    {
#pragma warning disable IDE1006 // 命名样式
        public long user_id { get; set; } = user_id;
        public long type { get; set; } = type;
        public string daily { get; set; } = daily;
#pragma warning restore IDE1006 // 命名样式
    }
}

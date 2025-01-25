using System.Text.Json.Serialization;
using Milimoe.FunGame.Core.Api.Utility;

namespace Milimoe.RainBOT.Settings
{
    public class QQOpenID
    {
        public static Dictionary<string, long> QQAndOpenID { get; set; } = [];

        public static PluginConfig Configs { get; set; } = new("rainbot", "qqopenid");

        public static void LoadConfig()
        {
            Configs.LoadConfig();
            foreach (string str in Configs.Keys)
            {
                if (Configs.TryGetValue(str, out object? value) && value is long qq && qq != 0)
                {
                    QQAndOpenID.TryAdd(str, qq);
                }
            }
        }

        public static void SaveConfig()
        {
            lock (Configs)
            {
                Configs.Clear();
                foreach (string openid in QQAndOpenID.Keys)
                {
                    Configs.Add(openid, QQAndOpenID[openid]);
                }
                Configs.SaveConfig();
            }
        }

        public static string Bind(string openid, long qq)
        {
            if (openid.Trim() == "" || qq <= 0)
            {
                return "请输入正确的OpenID和QQ！";
            }

            if (QQAndOpenID.Any(kv => kv.Value == qq || kv.Key != openid))
            {
                return $"此接入码已被其他人绑定或者你的QQ已经绑定了其他的接入码。如果你是此接入码的主人，请联系客服处理。";
            }

            if (QQAndOpenID.TryAdd(openid, qq))
            {
                SaveConfig();
            }
            else
            {
                return $"绑定失败，请稍后再试！如持续绑定失败请联系客服处理。";
            }

            return "绑定成功！如果需要解除绑定，请发送【解绑+接入码】！";
        }

        public static string Unbind(string openid, long qq)
        {
            if (QQOpenID.QQAndOpenID.TryGetValue(openid, out long bindqq) && bindqq == qq && QQOpenID.QQAndOpenID.Remove(openid))
            {
                return NetworkUtility.JsonSerialize($"解绑成功！");
            }

            return NetworkUtility.JsonSerialize("解绑失败！没有查到绑定的信息或者此账号已被其他人绑定，如果你是此接入码的主人，请联系客服处理。");
        }
    }

    public class ThirdPartyMessage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("isgroup")]
        public bool IsGroup { get; set; } = false;

        [JsonPropertyName("detail")]
        public string Detail { get; set; } = "";

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = "";

        [JsonPropertyName("openid")]
        public string OpenId { get; set; } = "";

        [JsonPropertyName("authoropenid")]
        public string AuthorOpenId { get; set; } = "";
    }
}

using System.Text.Json;
using Milimoe.OneBot.Framework;
using Milimoe.OneBot.Framework.Utility;
using Milimoe.OneBot.Model.Content;
using Milimoe.OneBot.Model.Other;

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
            HttpResponseMessage msg =  await HTTPPost.Post(SupportedAPI.get_group_list, new GroupMessageContent(0));
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
    }
}

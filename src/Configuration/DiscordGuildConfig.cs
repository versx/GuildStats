namespace GuildStats.Configuration
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class DiscordGuildConfig
    {
        [JsonProperty("ownerId")]
        public ulong OwnerId { get; set; }

        [JsonProperty("guildId")]
        public ulong GuildId { get; set; }

        //[JsonProperty("categoryChannelId")]
        //public ulong CategoryChannelId { get; set; }

        [JsonProperty("memberCountChannelId")]
        public ulong MemberCountChannelId { get; set; }

        [JsonProperty("botCountChannelId")]
        public ulong BotCountChannelId { get; set; }

        [JsonProperty("roleCountChannelId")]
        public ulong RoleCountChannelId { get; set; }

        [JsonProperty("channelCountChannelId")]
        public ulong ChannelCountChannelId { get; set; }

        [JsonProperty("memberRoles")]
        public Dictionary<ulong, MemberRoleConfig> MemberRoles { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        public DiscordGuildConfig()
        {
            MemberRoles = new Dictionary<ulong, MemberRoleConfig>();
        }
    }

    public class MemberRoleConfig
    {
        [JsonProperty("roleIds")]
        public List<ulong> RoleIds { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        public MemberRoleConfig()
        {
            RoleIds = new List<ulong>();
        }
    }
}
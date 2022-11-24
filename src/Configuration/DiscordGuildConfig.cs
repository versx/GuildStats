namespace GuildStats.Configuration
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    using Microsoft.Extensions.Logging;

    public class DiscordGuildConfig
    {
        [JsonPropertyName("ownerId")]
        public ulong OwnerId { get; set; }

        [JsonPropertyName("guildId")]
        public ulong GuildId { get; set; }

        //[JsonPropertyName("categoryChannelId")]
        //public ulong CategoryChannelId { get; set; }

        [JsonPropertyName("memberCountChannelId")]
        public ulong MemberCountChannelId { get; set; }

        [JsonPropertyName("botCountChannelId")]
        public ulong BotCountChannelId { get; set; }

        [JsonPropertyName("roleCountChannelId")]
        public ulong RoleCountChannelId { get; set; }

        [JsonPropertyName("channelCountChannelId")]
        public ulong ChannelCountChannelId { get; set; }

        [JsonPropertyName("memberRoles")]
        public IReadOnlyDictionary<ulong, MemberRoleConfig> MemberRoles { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the event logging level to set for the Discord Guild.
        /// </summary>
        [JsonPropertyName("logLevel")]
        public LogLevel LogLevel { get; set; }

        public DiscordGuildConfig()
        {
            MemberRoles = new Dictionary<ulong, MemberRoleConfig>();
        }
    }

    public class MemberRoleConfig
    {
        [JsonPropertyName("roleIds")]
        public IReadOnlyList<ulong> RoleIds { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        public MemberRoleConfig()
        {
            RoleIds = new List<ulong>();
        }
    }
}
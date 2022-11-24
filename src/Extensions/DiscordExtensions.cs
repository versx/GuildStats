namespace GuildStats.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    using GuildStats.Configuration;
    using GuildStats.Diagnostics;

    public static class DiscordExtensions
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger(nameof(DiscordExtensions));

        public static int GetMemberRoleCount(this IEnumerable<DiscordMember> members, ulong roleId)
        {
            try
            {
                var count = members.Count(x => x.Roles.Select(role => role.Id)
                                                      .Contains(roleId));
                return count;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            return 0;
        }

        public static async Task UpdateChannelNameAsync(this DiscordGuild guild, ulong channelId, string newName)
        {
            var channel = guild.GetChannel(channelId);
            if (channel == null)
            {
                _logger.Error($"Failed to find channel with id {channelId}");
                return;
            }

            _logger.Debug($"Updating channel name: {channelId} '{channel.Name}'=>''{newName}");
            await channel.ModifyAsync(action => action.Name = newName);

            // Wait half a second between updating channel names
            Thread.Sleep(500);
        }

        public static IReadOnlyDictionary<DiscordChannel, string> GetGuildMemberRoleCounts(this DiscordGuild guild, IReadOnlyDictionary<ulong, MemberRoleConfig> memberRoles)
        {
            var result = new Dictionary<DiscordChannel, string>();

            // Post individual role counts
            foreach (var (roleChannelId, roleConfig) in memberRoles)
            {
                var roleChannel = guild.GetChannel(roleChannelId);
                if (roleChannel == null)
                {
                    _logger.Error($"Failed to find role channel with id {roleChannelId}");
                    continue;
                }

                var total = guild.GetGuildRolesCount(roleConfig.RoleIds);
                result.Add(roleChannel, $"{roleConfig.Text}: {total:N0}");

                Thread.Sleep(500);
            }

            return result;
        }

        public static int GetGuildRolesCount(this DiscordGuild guild, IEnumerable<ulong> roleIds)
        {
            var total = 0;
            foreach (var roleId in roleIds)
            {
                var role = guild.GetRole(roleId);
                if (role == null)
                {
                    _logger.Error($"Failed to find role with id '{roleId}'");
                    continue;
                }

                total += guild.Members.Values.GetMemberRoleCount(role.Id);
                Thread.Sleep(500);
            }

            return total;
        }
    }
}
namespace GuildStats
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using GuildStats.Configuration;
    using GuildStats.Diagnostics;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    public class Bot
    {
        #region Variables

        private readonly Dictionary<ulong, DiscordClient> _servers;
        private readonly Config _whConfig;

        private static readonly IEventLogger _logger = EventLogger.GetLogger("BOT");

        #endregion

        #region Constructor

        /// <summary>
        /// Discord bot class
        /// </summary>
        /// <param name="config">Configuration settings</param>
        public Bot(Config whConfig)
        {
            _logger.Trace($"WhConfig [Servers={whConfig.Servers.Count}]");
            _servers = new Dictionary<ulong, DiscordClient>();
            _whConfig = whConfig;

            // Set unhandled exception event handler
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            // Create a DiscordClient object per Discord server in config
            var keys = _whConfig.Servers.Keys.ToList();
            for (var i = 0; i < keys.Count; i++)
            {
                var guildId = keys[i];
                var server = _whConfig.Servers[guildId];
                var client = new DiscordClient(new DiscordConfiguration
                {
                    AutomaticGuildSync = true,
                    AutoReconnect = true,
                    EnableCompression = true,
                    Token = server.Token,
                    TokenType = TokenType.Bot,
                    UseInternalLogHandler = true
                });

                // If you are on Windows 7 and using .NETFX, install 
                // DSharpPlus.WebSocket.WebSocket4Net from NuGet,
                // add appropriate usings, and uncomment the following
                // line
                //client.SetWebSocketClient<WebSocket4NetClient>();

                // If you are on Windows 7 and using .NET Core, install 
                // DSharpPlus.WebSocket.WebSocket4NetCore from NuGet,
                // add appropriate usings, and uncomment the following
                // line
                //client.SetWebSocketClient<WebSocket4NetCoreClient>();

                // If you are using Mono, install 
                // DSharpPlus.WebSocket.WebSocketSharp from NuGet,
                // add appropriate usings, and uncomment the following
                // line
                //client.SetWebSocketClient<WebSocketSharpClient>();

                client.Ready += Client_Ready;
                client.GuildAvailable += Client_GuildAvailable;
                //_client.MessageCreated += Client_MessageCreated;
                client.ClientErrored += Client_ClientErrored;
                client.DebugLogger.LogMessageReceived += DebugLogger_LogMessageReceived;

                _logger.Info($"Configured Discord server {guildId}");
                if (!_servers.ContainsKey(guildId))
                {
                    _servers.Add(guildId, client);
                }

                // Wait 3 seconds between initializing Discord clients
                Task.Delay(3000).GetAwaiter().GetResult();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Start the Discord bot(s)
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            _logger.Trace("Start");
            _logger.Info("Connecting to Discord...");

            // Loop through each Discord server and attempt initial connection
            var keys = _servers.Keys.ToList();
            for (var i = 0; i < keys.Count; i++)
            {
                var guildId = keys[i];
                var client = _servers[guildId];

                _logger.Info($"Attempting connection to Discord server {guildId}");
                await client.ConnectAsync();
                await Task.Delay(1000);
            }

            _logger.Info("GuildStats is running...");
        }

        /// <summary>
        /// Stop the Discord bot(s)
        /// </summary>
        /// <returns></returns>
        public async Task Stop()
        {
            _logger.Trace("Stop");
            _logger.Info("Disconnecting from Discord...");

            // Loop through each Discord server and terminate the connection
            var keys = _servers.Keys.ToList();
            for (var i = 0; i < keys.Count; i++)
            {
                var guildId = keys[i];
                var client = _servers[guildId];

                _logger.Info($"Attempting connection to Discord server {guildId}");
                await client.DisconnectAsync();
                await Task.Delay(1000);
            }

            _logger.Info("WebhookManager is stopped...");
        }

        #endregion

        #region Discord Events

        private Task Client_Ready(ReadyEventArgs e)
        {
            _logger.Info($"------------------------------------------");
            _logger.Info($"[DISCORD] Connected.");
            _logger.Info($"[DISCORD] ----- Current Application");
            _logger.Info($"[DISCORD] Name: {e.Client.CurrentApplication.Name}");
            _logger.Info($"[DISCORD] Description: {e.Client.CurrentApplication.Description}");
            _logger.Info($"[DISCORD] Owner: {e.Client.CurrentApplication.Owner.Username}#{e.Client.CurrentApplication.Owner.Discriminator}");
            _logger.Info($"[DISCORD] ----- Current User");
            _logger.Info($"[DISCORD] Id: {e.Client.CurrentUser.Id}");
            _logger.Info($"[DISCORD] Name: {e.Client.CurrentUser.Username}#{e.Client.CurrentUser.Discriminator}");
            _logger.Info($"[DISCORD] Email: {e.Client.CurrentUser.Email}");
            _logger.Info($"------------------------------------------");

            return Task.CompletedTask;
        }

        private async Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            // If guild is in configured servers list then attempt to create emojis needed
            if (_whConfig.Servers.ContainsKey(e.Guild.Id))
            {
                await UpdateGuildStats(e.Guild);

                if (!(e.Client is DiscordClient client))
                {
                    _logger.Error($"DiscordClient is null, Unable to update status.");
                    return;
                }

                // Set custom bot status if guild is in config server list
                if (_whConfig.Servers.ContainsKey(e.Guild.Id))
                {
                    var status = _whConfig.Servers[e.Guild.Id].Status;
                    await client.UpdateStatusAsync(new DiscordGame(status ?? $"v{Strings.Version}"), UserStatus.Online);
                }
            }
        }

        private async Task Client_ClientErrored(ClientErrorEventArgs e)
        {
            _logger.Error(e.Exception);

            await Task.CompletedTask;
        }

        private void DebugLogger_LogMessageReceived(object sender, DebugLogMessageEventArgs e)
        {
            if (e.Application == "REST")
            {
                _logger.Error("[DISCORD] RATE LIMITED-----------------");
                return;
            }

            //Color
            ConsoleColor color;
            switch (e.Level)
            {
                case DSharpPlus.LogLevel.Error: color = ConsoleColor.DarkRed; break;
                case DSharpPlus.LogLevel.Warning: color = ConsoleColor.Yellow; break;
                case DSharpPlus.LogLevel.Info: color = ConsoleColor.White; break;
                case DSharpPlus.LogLevel.Critical: color = ConsoleColor.Red; break;
                case DSharpPlus.LogLevel.Debug: default: color = ConsoleColor.DarkGray; break;
            }

            //Source
            var sourceName = e.Application;

            //Text
            var text = e.Message;

            //Build message
            var builder = new System.Text.StringBuilder(text.Length + (sourceName?.Length ?? 0) + 5);
            if (sourceName != null)
            {
                builder.Append('[');
                builder.Append(sourceName);
                builder.Append("] ");
            }

            for (var i = 0; i < text.Length; i++)
            {
                //Strip control chars
                var c = text[i];
                if (!char.IsControl(c))
                    builder.Append(c);
            }

            if (text != null)
            {
                builder.Append(": ");
                builder.Append(text);
            }

            text = builder.ToString();
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        #endregion

        #region Private Methods

        private async Task UpdateGuildStats(DiscordGuild guild)
        {
            if (!_whConfig.Servers.ContainsKey(guild.Id))
                return;

            var server = _whConfig.Servers[guild.Id];
            var memberCountChannel = guild.GetChannel(server.MemberCountChannelId);
            if (memberCountChannel == null)
            {
                _logger.Error($"Failed to find member count channel with id {server.MemberCountChannelId}");
                return;
            }
            var botCountChannel = guild.GetChannel(server.BotCountChannelId);
            if (botCountChannel == null)
            {
                _logger.Error($"Failed to find bot count channel with id {server.BotCountChannelId}");
                return;
            }
            var roleCountChannel = guild.GetChannel(server.RoleCountChannelId);
            if (roleCountChannel == null)
            {
                _logger.Error($"Failed to find role count channel with id {server.RoleCountChannelId}");
                return;
            }
            var channelCountChannel = guild.GetChannel(server.ChannelCountChannelId);
            if (channelCountChannel == null)
            {
                _logger.Error($"Failed to find channel count channel with id {server.ChannelCountChannelId}");
                return;
            }

            await memberCountChannel.ModifyAsync($"Member Count: {guild.MemberCount:N0}");
            await botCountChannel.ModifyAsync($"Bot Count: {guild.Members.Where(x => x.IsBot).ToList().Count:N0}");
            await roleCountChannel.ModifyAsync($"Role Count: {guild.Roles.Count:N0}");
            await channelCountChannel.ModifyAsync($"Channel Count: {guild.Channels.Count:N0}");

            foreach (var item in server.MemberRoles)
            {
                var roleChannelId = item.Key;
                var roleConfig = item.Value;
                var roleChannel = guild.GetChannel(roleChannelId);
                if (roleChannel == null)
                {
                    _logger.Error($"Failed to find role channel with id {roleChannelId}");
                    continue;
                }
                var total = 0;
                foreach (var roleId in roleConfig.RoleIds)
                {
                    var role = guild.GetRole(roleId);
                    if (role == null)
                    {
                        _logger.Error($"Failed to find role with id {roleId}");
                        continue;
                    }
                    total += GetMemberRoleCount(role.Id, guild.Members.ToList());
                }
                await roleChannel.ModifyAsync($"{roleConfig.Text}: {total:N0}");
            }
        }

        private int GetMemberRoleCount(ulong roleId, List<DiscordMember> members)
        {
            var count = 0;
            try
            {
                members.ForEach(x =>
                {
                    var roleIds = x.Roles.Select(role => role.Id);
                    if (roleIds.Contains(roleId))
                        count++;
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            return count;
        }

        private async void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.Debug("Unhandled exception caught.");
            _logger.Error((Exception)e.ExceptionObject);

            if (e.IsTerminating)
            {
                var keys = _whConfig.Servers.Keys.ToList();
                for (var i = 0; i < keys.Count; i++)
                {
                    var guildId = keys[i];
                    if (!_whConfig.Servers.ContainsKey(guildId))
                    {
                        _logger.Error($"Unable to find guild id {guildId} in server config list.");
                        continue;
                    }
                    var server = _whConfig.Servers[guildId];

                    if (!_servers.ContainsKey(guildId))
                    {
                        _logger.Error($"Unable to find guild id {guildId} in Discord server client list.");
                        continue;
                    }
                    var client = _servers[guildId];
                    if (client != null)
                    {
                        var owner = await client.GetUserAsync(server.OwnerId);
                        if (owner == null)
                        {
                            _logger.Warn($"Unable to get owner from id {server.OwnerId}.");
                            return;
                        }

                        // TODO: await client.SendDirectMessage(owner, Strings.CrashMessage, null);
                    }
                }
            }
        }

        #endregion
    }
}
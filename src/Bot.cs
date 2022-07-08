namespace GuildStats
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;
    using Thread = System.Threading.Thread;
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
        private readonly Config _config;
        private readonly Timer _timer;

        private static readonly IEventLogger _logger = EventLogger.GetLogger("BOT");

        #endregion

        #region Constructor

        /// <summary>
        /// Discord bot class
        /// </summary>
        /// <param name="config">Configuration settings</param>
        public Bot(Config config)
        {
            _logger.Trace($"Config [Servers={config.Servers.Count}]");

            _servers = new Dictionary<ulong, DiscordClient>();
            _config = config;
            _timer = new Timer { Interval = 60 * 1000 * _config.UpdateIntervalM };
            _timer.Elapsed += async (sender, e) => await OnTimerElapsedAsync();

            // Set unhandled exception event handler
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandlerAsync;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Start the Discord bot(s)
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync()
        {
            _logger.Trace("Start");

            _logger.Info($"Initializing {_config.Servers.Count:N0} Discord server clients...");
            await InitializeAsync();

            _logger.Info("Connecting to Discord...");

            // Loop through each Discord server and attempt initial connection
            foreach (var (guildId, guildClient) in _servers)
            {
                _logger.Info($"Attempting connection to Discord server {guildId}");
                await guildClient.ConnectAsync();
                await Task.Delay(1000);
            }

            // Start update timer
            _timer.Start();

            _logger.Info($"{Strings.BotName} is running...");
        }

        /// <summary>
        /// Stop the Discord bot(s)
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            _logger.Trace("Stop");
            _logger.Info("Disconnecting from Discord...");

            // Loop through each Discord server and terminate the connection
            foreach (var (guildId, guildClient) in _servers)
            {
                _logger.Info($"Attempting to disconnect from Discord server {guildId}");
                await guildClient.DisconnectAsync();
                await Task.Delay(1000);
            }

            // Stop update timer
            _timer.Stop();

            _logger.Info($"{Strings.BotName} is stopped...");
        }

        #endregion

        #region Discord Events

        private Task Client_Ready(DiscordClient sender, ReadyEventArgs e)
        {
            _logger.Info($"------------------------------------------");
            _logger.Info($"[DISCORD] Connected.");
            _logger.Info($"[DISCORD] ----- Current Application");
            _logger.Info($"[DISCORD] Name: {sender.CurrentApplication.Name}");
            _logger.Info($"[DISCORD] Description: {sender.CurrentApplication.Description}");
            var owner = sender.CurrentApplication.Owners.FirstOrDefault();
            _logger.Info($"[DISCORD] Owner: {owner?.Username}#{owner?.Discriminator}");
            _logger.Info($"[DISCORD] ----- Current User");
            _logger.Info($"[DISCORD] Id: {sender.CurrentUser.Id}");
            _logger.Info($"[DISCORD] Name: {sender.CurrentUser.Username}#{sender.CurrentUser.Discriminator}");
            _logger.Info($"[DISCORD] Email: {sender.CurrentUser.Email}");
            _logger.Info($"------------------------------------------");

            return Task.CompletedTask;
        }

        private async Task Client_GuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
        {
            // If guild is in configured servers list then attempt to create emojis needed
            if (!_config.Servers.ContainsKey(e.Guild.Id))
                return;

            if (sender is not DiscordClient client)
            {
                _logger.Error($"DiscordClient is null, Unable to update status.");
                return;
            }

            // Set custom bot status if guild is in config server list
            var status = _config.Servers[e.Guild.Id].Status;
            await client.UpdateStatusAsync(new DiscordActivity(status ?? $"v{Strings.Version}"), UserStatus.Online);

            await UpdateGuildStatsAsync(e.Guild);
        }

        private async Task Client_ClientErrored(DiscordClient sender, ClientErrorEventArgs e)
        {
            _logger.Error(e.Exception);

            await Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private async Task InitializeAsync()
        {
            var clientIntents = DiscordIntents.DirectMessages
                | DiscordIntents.DirectMessageTyping
                | DiscordIntents.GuildEmojis
                | DiscordIntents.GuildMembers
                | DiscordIntents.GuildMessages
                | DiscordIntents.GuildMessageTyping
                | DiscordIntents.GuildPresences
                | DiscordIntents.Guilds
                | DiscordIntents.GuildWebhooks;

            // Create a DiscordClient object per Discord server in config
            foreach (var (guildId, guildConfig) in _config.Servers)
            {
                if (string.IsNullOrEmpty(guildConfig.Token))
                {
                    // Check if there's only one server configured
                    if (_config.Servers.Count == 1)
                    {
                        _logger.Error($"Bot token for guild {guildId} cannot be empty and must be set, existing application.");
                        Environment.Exit(-1);
                    }

                    _logger.Error($"Bot token for guild {guildId} cannot be empty and must be set, skipping guild.");
                    continue;
                }

                var client = new DiscordClient(new DiscordConfiguration
                {
                    AutoReconnect = true,
                    AlwaysCacheMembers = true,
                    // REVIEW: Hmm maybe we should compress the whole stream instead of just payload.
                    GatewayCompressionLevel = GatewayCompressionLevel.Payload,
                    Token = guildConfig?.Token,
                    TokenType = TokenType.Bot,
                    MinimumLogLevel = guildConfig.LogLevel,
                    Intents = clientIntents,
                    ReconnectIndefinitely = true,
                });

                client.Ready += Client_Ready;
                client.GuildAvailable += Client_GuildAvailable;
                //_client.MessageCreated += Client_MessageCreated;
                client.ClientErrored += Client_ClientErrored;

                _logger.Info($"Configured Discord server {guildId}");
                if (!_servers.ContainsKey(guildId))
                {
                    _servers.Add(guildId, client);
                }

                // Wait 3 seconds between initializing Discord clients
                await Task.Delay(3000);
            }
        }

        private async Task UpdateGuildStatsAsync(DiscordGuild guild)
        {
            if (!_config.Servers.ContainsKey(guild.Id))
                return;

            _logger.Debug($"Updating guild statistic channels for guild: {guild.Name} ({guild.Id})");

            var server = _config.Servers[guild.Id];

            await UpdateChannelNameAsync(guild, server.MemberCountChannelId, $"Member Count: {guild.MemberCount:N0}");
            await UpdateChannelNameAsync(guild, server.BotCountChannelId, $"Bot Count: {guild.Members.Where(x => x.Value.IsBot).ToList().Count:N0}");
            await UpdateChannelNameAsync(guild, server.RoleCountChannelId, $"Role Count: {guild.Roles.Count:N0}");
            await UpdateChannelNameAsync(guild, server.ChannelCountChannelId, $"Channel Count: {guild.Channels.Count:N0}");

            foreach (var (roleChannelId, roleConfig) in server.MemberRoles)
            {
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
                    total += GetMemberRoleCount(role.Id, guild.Members.Values.ToList());
                    Thread.Sleep(500);
                }

                await roleChannel.ModifyAsync(action => action.Name = $"{roleConfig.Text}: {total:N0}");
                Thread.Sleep(500);
            }

            // Wait 2 seconds per guild
            Thread.Sleep(2000);
        }

        private static async Task UpdateChannelNameAsync(DiscordGuild guild, ulong channelId, string newName)
        {
            var channel = guild.GetChannel(channelId);
            if (channel == null)
            {
                _logger.Error($"Failed to find channel with id {channelId}");
                return;
            }

            _logger.Debug($"Updating channel: {channelId} '{channel.Name}'=>''{newName}");
            await channel.ModifyAsync(action => action.Name = newName);

            // Wait half a second between updating channel names
            Thread.Sleep(500);
        }

        private static int GetMemberRoleCount(ulong roleId, List<DiscordMember> members)
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
                return 0;
            }
        }

        private async Task OnTimerElapsedAsync()
        {
            // Loop all configured Discord server clients
            foreach (var (_, guildClient) in _servers)
            {
                // Filter only guilds we have configured
                var guilds = guildClient.Guilds.Values.Where(guild => _config.Servers.ContainsKey(guild.Id));
                foreach (var guild in guilds)
                {
                    await UpdateGuildStatsAsync(guild);
                    // Wait 5 seconds per guild
                    Thread.Sleep(5000);
                }
            }
        }

        private async void UnhandledExceptionHandlerAsync(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.Debug("Unhandled exception caught.");
            _logger.Error((Exception)e.ExceptionObject);

            if (!e.IsTerminating)
                return;

            foreach (var (guildId, guildConfig) in _config.Servers)
            {
                if (!_config.Servers.ContainsKey(guildId))
                {
                    _logger.Error($"Unable to find guild id {guildId} in server config list.");
                    continue;
                }

                if (!_servers.ContainsKey(guildId))
                {
                    _logger.Error($"Unable to find guild id {guildId} in Discord server client list.");
                    continue;
                }
                var client = _servers[guildId];
                if (client == null)
                    continue;

                if (!client.Guilds.ContainsKey(guildId))
                {
                    _logger.Error($"Unable to find guild id {guildId} in Discord client guilds list that should have it.");
                    continue;
                }
                var guild = client.Guilds[guildId];

                var owner = await guild.GetMemberAsync(guildConfig.OwnerId);
                if (owner == null)
                {
                    _logger.Warn($"Unable to find owner with id {guildConfig.OwnerId}.");
                    continue;
                }

                // Send crash message to guild owner
                await owner.SendMessageAsync(Strings.CrashMessage);
            }
        }

        #endregion
    }
}
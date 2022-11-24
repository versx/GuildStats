namespace GuildStats
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Timer = System.Timers.Timer;
    using System.Threading;
    using System.Threading.Tasks;

    using GuildStats.Configuration;
    using GuildStats.Diagnostics;
    using GuildStats.Extensions;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    // TODO: Localize text

    public class Bot
    {
        #region Constants

        private const int OneMinuteMs = 60 * 1000;
        private const int UpdateGuildStatisticsIntervalS = 5; // 5 seconds
        private const int DelayUpdateBetweenGuildsS = 2; // 2 seconds

        #endregion

        #region Variables

        private readonly Dictionary<ulong, DiscordClient> _servers;
        private readonly Config _config;
        private readonly Timer _timer;

        private static readonly IEventLogger _logger = EventLogger.GetLogger(nameof(Bot));

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
            _timer = new Timer { Interval = OneMinuteMs * _config.UpdateIntervalM };
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

            await InitializeAsync();

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

            // Ensure we have a client for the guild
            if (!_servers.ContainsKey(e.Guild.Id))
                return;

            // Set custom bot status for guild
            var client = _servers[e.Guild.Id];
            var status = _config.Servers[e.Guild.Id].Status;
            var activity = new DiscordActivity(status ?? $"v{Strings.Version}");
            await client.UpdateStatusAsync(activity, UserStatus.Online);

            new Thread(async () => await UpdateGuildStatsAsync(e.Guild))
            { IsBackground = true }.Start();

            await Task.CompletedTask;
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
            _logger.Info($"Initializing {_config.Servers.Count:N0} Discord server clients...");

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

            await guild.UpdateChannelNameAsync(server.MemberCountChannelId, $"Member Count: {guild.MemberCount:N0}");
            await guild.UpdateChannelNameAsync(server.BotCountChannelId, $"Bot Count: {guild.Members.Where(x => x.Value.IsBot).ToList().Count:N0}");
            await guild.UpdateChannelNameAsync(server.RoleCountChannelId, $"Role Count: {guild.Roles.Count:N0}");
            await guild.UpdateChannelNameAsync(server.ChannelCountChannelId, $"Channel Count: {guild.Channels.Count:N0}");

            if (!(server.MemberRoles?.Any() ?? false))
                return;

            var roleChannelNames = guild.GetGuildMemberRoleCounts(server.MemberRoles);
            foreach (var (rolesChannel, text) in roleChannelNames)
            {
                await rolesChannel.ModifyAsync(action => action.Name = text);
                Thread.Sleep(500);
            }

            // Wait 2 seconds per guild
            Thread.Sleep(DelayUpdateBetweenGuildsS * 1000);
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
                    Thread.Sleep(UpdateGuildStatisticsIntervalS * 1000);
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
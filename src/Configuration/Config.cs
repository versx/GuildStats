namespace GuildStats.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Newtonsoft.Json;

    using GuildStats.Diagnostics;

    /// <summary>
    /// Configuration file class
    /// </summary>
    public class Config
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("CONFIG");

        /// <summary>
        /// Gets or sets the Discord servers configuration
        /// </summary>
        [JsonProperty("servers")]
        public Dictionary<ulong, DiscordGuildConfig> Servers { get; set; }

        /// <summary>
        /// Gets or sets the interval of how frequent to update the guild stats
        /// </summary>
        [JsonProperty("updateIntervalM")]
        public int UpdateIntervalM { get; set; }

        /// <summary>
        /// Gets or sets the event logging level to set
        /// </summary>
        [JsonProperty("logLevel")]
        public LogLevel LogLevel { get; set; }

        /// <summary>
        /// Gets or sets the configuration file path
        /// </summary>
        [JsonIgnore]
        public string FileName { get; set; }

        /// <summary>
        /// Instantiate a new <see cref="WhConfig"/> class
        /// </summary>
        public Config()
        {
            LogLevel = LogLevel.Trace;
            Servers = new Dictionary<ulong, DiscordGuildConfig>();
            UpdateIntervalM = 10;
        }

        /// <summary>
        /// Save the current configuration object
        /// </summary>
        /// <param name="filePath">Path to save the configuration file</param>
        public void Save(string filePath)
        {
            var data = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, data);
        }

        /// <summary>
        /// Load the configuration from a file
        /// </summary>
        /// <param name="filePath">Path to load the configuration file from</param>
        /// <returns>Returns the deserialized configuration object</returns>
        public static Config Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Config not loaded because file not found.", filePath);
            }

            var config = LoadInit<Config>(filePath);
            return config;
        }

        public static T LoadInit<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{filePath} file not found.", filePath);
            }

            var data = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(data))
            {
                _logger.Error($"{filePath} database is empty.");
                return default;
            }

            return (T)JsonConvert.DeserializeObject(data, typeof(T));
        }
    }
}

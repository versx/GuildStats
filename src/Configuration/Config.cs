namespace GuildStats.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;

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
        [JsonPropertyName("servers")]
        public Dictionary<ulong, DiscordGuildConfig> Servers { get; set; }

        /// <summary>
        /// Gets or sets the interval of how frequent to update the guild stats
        /// </summary>
        [JsonPropertyName("updateIntervalM")]
        public int UpdateIntervalM { get; set; }

        /// <summary>
        /// Gets or sets the event logging level to set
        /// </summary>
        [JsonPropertyName("logLevel")]
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
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip,
                WriteIndented = true,
            });
            File.WriteAllText(filePath, json);
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

            var json = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(json))
            {
                _logger.Error($"{filePath} database is empty.");
                return default;
            }

            var obj = JsonSerializer.Deserialize<T>(json);
            return obj;
        }
    }
}

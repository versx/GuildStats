namespace GuildStats
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    class Program
    {
        public static string ManagerName { get; set; } = "Main";

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        /// <summary>
        /// Asynchronous main entry point
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns></returns>
        static async Task MainAsync(string[] args)
        {
            var configPath = Path.Combine(Environment.CurrentDirectory, Strings.ConfigFileName);
            var logger = Diagnostics.EventLogger.GetLogger(ManagerName);
            var config = Configuration.Config.Load(configPath);
            if (config == null)
            {
                logger.Error($"Failed to load config {configPath}.");
                return;
            }
            config.FileName = configPath;

            // Start bot
            var bot = new Bot(config);
            await bot.StartAsync();

            // Keep the process alive
            Process.GetCurrentProcess().WaitForExit();
        }
    }
}
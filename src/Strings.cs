namespace GuildStats
{
    public static class Strings
    {
        public static readonly string BotName = "Guild Stats";

        public static readonly string Version = System.Reflection.Assembly.GetExecutingAssembly().GetName()?.Version?.ToString() ?? "1.1.0.0";

        public const string ConfigFileName = "config.json";
        public const string LogsFolder = "logs";

        public const string CrashMessage = "GUILD STATS JUST CRASHED!";
    }
}
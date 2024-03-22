using Discord;

namespace FissionRevamped.Services
{
    public class LoggingService
    {
        public static Task HandleLog(LogMessage message)
        {
            Console.Write("[*] ");
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = message.Severity switch
            {
                LogSeverity.Critical or LogSeverity.Error => ConsoleColor.Red,
                LogSeverity.Warning => ConsoleColor.Yellow,
                _ => ConsoleColor.White
            };
            Console.WriteLine("[{1}] {0} {2}", message.Message, message.Severity, message.Exception);
            Console.ForegroundColor = defaultColor;
            return Task.CompletedTask;
        }

        public static void LogInfo(string message)
        {
            Console.Write("[*] ");
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[Info] {0}", message);
            Console.ForegroundColor = defaultColor;
        }
        public static void LogError(string message)
        {
            Console.Write("[*] ");
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[Info] {0}", message);
            Console.ForegroundColor = defaultColor;
        }
    }
}

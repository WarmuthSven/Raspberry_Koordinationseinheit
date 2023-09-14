// ReSharper disable once CheckNamespace
namespace LoraRaspberry
{
    internal static class Program
    {
        public static Action? programExits;
        private static bool _continue = true;

        public static void Main(string[] args)
        {
            Console.WriteLine("Start Program.");
            AppDomain.CurrentDomain.ProcessExit += ProcessExits;
            Console.CancelKeyPress += ConsoleCancel;
            SingletonWrapper.InitializeAll();

            StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;

            Console.WriteLine("Type QUIT to exit");

            while (_continue)
            {
                string? message = Console.ReadLine();

                if (stringComparer.Equals("quit", message))
                {
                    _continue = false;
                }
            }
        }

        private static void ProcessExits(object? sender, EventArgs events)
        {
            Console.WriteLine("\n\nApp is shutting down!\n");
            programExits?.Invoke();
            Console.WriteLine("App is properly shutdown.\n\n");
        }
        private static void ConsoleCancel(object? sender, ConsoleCancelEventArgs events)
        {
            Console.WriteLine("\n\nApp is shutting down!");
            events.Cancel = true;
            programExits?.Invoke();
            Console.WriteLine("\nApp is properly shutdown.\n\n");
            events.Cancel = false;
        }
    }
}

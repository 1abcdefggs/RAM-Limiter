using System;

namespace RAMLIMITER
{
    static class Log
    {
        internal static void Write(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}

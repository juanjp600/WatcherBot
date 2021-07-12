using System;
using System.Threading.Tasks;
using Discord;

namespace Bot600.Utils
{
    public static class BarotraumaToolBox
    {
        internal static async Task InternalLog(LogMessage msg)
        {
            await Task.Yield();

            Console.WriteLine($"[{msg.Severity}] {msg.Source}: {msg.Message}");
            if (msg.Exception is not null)
            {
                Console.WriteLine($"Exception: {msg.Exception.Message} {msg.Exception.StackTrace}");
            }
        }

        public static int CountSubstrings(this string str, string substr)
        {
            var count = 0;
            var index = 0;
            while (true)
            {
                index = str.IndexOf(substr, index, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    break;
                }

                index++;
                count++;
            }

            return count;
        }

        public static bool ToBool(this IsCringe cringe)
        {
            return cringe == IsCringe.Yes;
        }

        public static IsCringe ToCringe(this bool @bool)
        {
            return @bool ? IsCringe.Yes : IsCringe.No;
        }
    }
}

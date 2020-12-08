using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Bot600
{
    class Program
    {
        public static void Main(string[] args)
        => new BotMain().MainAsync().GetAwaiter().GetResult();
    }
}

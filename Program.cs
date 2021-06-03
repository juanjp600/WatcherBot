using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Bot600
{
    class Program
    {
        public static async Task Main(string[] args)
        => await new BotMain().MainAsync();
    }
}

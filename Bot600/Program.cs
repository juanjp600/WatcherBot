using System.Threading.Tasks;

namespace Bot600
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            await new BotMain().MainAsync();
        }
    }
}

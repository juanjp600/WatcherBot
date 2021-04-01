using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bot600.Commands
{
    public class HyenaCommandModule : ModuleBase<SocketCommandContext>
    {
        [Command("hyena", RunMode = RunMode.Async)]
        [Summary("smh my head")]
        [Alias("yeen")]
        public async Task Hyena()
        {
            Console.WriteLine("yeen command");
            var author = Context.Message.Author;
            var dm = await author.GetOrCreateDMChannelAsync();
            await dm.SendMessageAsync("https://tenor.com/view/keanu-reeves-knife-gif-19576998");
        }
    }
}

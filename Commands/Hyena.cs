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
        [Alias("yeen", "sus", "help")]
        public async Task Hyena()
        {
            var author = Context.Message.Author;
            var dm = await author.GetOrCreateDMChannelAsync();
            await dm.SendMessageAsync("https://tenor.com/view/keanu-reeves-knife-gif-19576998");
        }
    }
}

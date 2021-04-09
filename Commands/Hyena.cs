using Discord.Commands;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Bot600.Commands
{
    public class HyenaCommandModule : ModuleBase<SocketCommandContext>
    {
        [Command("sus", RunMode = RunMode.Async)]
        [Summary("smh my head")]
        public async Task Sus()
        {
            var author = Context.Message.Author;
            var dm = await author.GetOrCreateDMChannelAsync();
            await dm.SendMessageAsync("https://tenor.com/view/keanu-reeves-knife-gif-19576998");
        }

        [Command("help", RunMode = RunMode.Async)]
        [Summary("Lists commands that are available to all users.")]
        public async Task Help()
        {
            var author = Context.Message.Author;
            var dm = await author.GetOrCreateDMChannelAsync();
            await dm.SendMessageAsync(
                "```" +
                "!help: Shows this list of commands.\n" +
                "!c [hash]: Shows the description of a given commit on Barotrauma's private GitHub repository." +
                "```");
        }

        [Command("hyena", RunMode = RunMode.Async)]
        [Summary("hyena images")]
        [Alias("yeen")]
        public async Task Hyena()
        {
            var response = await WebRequest.Create("https://api.yeen.land").GetResponseAsync();
            await using var stream = response.GetResponseStream();
            if (stream is null) { return; }
            var reader = new StreamReader(stream);
            var r = await reader.ReadToEndAsync();
            var url = JsonConvert.DeserializeObject<HyenaUrl>(r);
            if (url is null) { return; }
            ReplyAsync(url.Url);
        }

        private class HyenaUrl
        {
            public HyenaUrl(string url)
            {
                Url = url;
            }

            public string Url { get; }
        }
    }
}

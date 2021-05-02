#nullable enable
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using Newtonsoft.Json;

namespace Bot600.Commands
{
    public class HyenaCommandModule : ModuleBase<SocketCommandContext>
    {
        private const string Endpoint = "https://api.yeen.land";
        private static readonly HttpClient HttpClient = new HttpClient();

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
            var reply = await GetReply(Endpoint);

            ReplyAsync(reply);
        }

        [Command("hyena", RunMode = RunMode.Async)]
        [Summary("hyena images")]
        [Alias("yeen")]
        public async Task Hyena(ulong id)
        {
            var requestUriString = $"{Endpoint}/id/{id}";
            var reply = await GetReply(requestUriString);

            ReplyAsync(reply);
        }

        private static async Task<HyenaUrl?> QueryApi(string requestUriString)
        {
            var response = await HttpClient.GetStringAsync(requestUriString);
            var url = JsonConvert.DeserializeObject<HyenaUrl>(response);

            return url;
        }

        private static async Task<string> GetReply(string requestUriString)
        {
            string reply;
            if (await QueryApi(requestUriString) is { } url)
                reply = url.Url;
            else
                reply = ":question:";

            return reply;
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

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using WatcherBot.Utils;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Newtonsoft.Json;

namespace WatcherBot.Commands
{
    // ReSharper disable once UnusedType.Global
    [Hidden]
    public class HyenaCommandModule : BaseCommandModule
    {
        private const string Endpoint = "https://api.yeen.land";
        private static readonly HttpClient HttpClient = new();

        [Command("roleicon")]
        public async Task SetRoleIcon(CommandContext ctx, DiscordRole role)
        {
            var icon = ctx.Message.Attachments.First();
            var wc = new WebClient();
            var s = wc.OpenRead(new Uri(icon.Url));
            var ms = new MemoryStream();
            s.CopyTo(ms);
            ms.Position = 0;
            await role.ModifyAsync(g => g.Icon = ms);
        }

        [Command("sus")]
        [Description("smh my head")]
        public async Task Sus(CommandContext context) =>
            await context.RespondDmAsync("https://tenor.com/view/keanu-reeves-knife-gif-19576998");

        [Command("hyena")]
        [Aliases("yeen")]
        [Description("hyena images")]
        public async Task Hyena(CommandContext context)
        {
            string reply = await GetReply(Endpoint);

            await context.RespondAsync(reply);
        }

        [Command("hyena")]
        [Description("fetch hyena images by id")]
        public async Task Hyena(CommandContext context, [Description("id of hyena photo to fetch")] ulong id)
        {
            var requestUriString = $"{Endpoint}/id/{id}";
            string reply = await GetReply(requestUriString);

            await context.RespondAsync(reply);
        }

        private static async Task<HyenaUrl?> QueryApi(string requestUriString)
        {
            string response = await HttpClient.GetStringAsync(requestUriString);
            var url = JsonConvert.DeserializeObject<HyenaUrl>(response);

            return url;
        }

        private static async Task<string> GetReply(string requestUriString)
        {
            string reply;
            if (await QueryApi(requestUriString) is { } url)
            {
                reply = url.Url;
            }
            else
            {
                reply = ":question:";
            }

            return reply;
        }

        private record HyenaUrl(string Url);
    }
}

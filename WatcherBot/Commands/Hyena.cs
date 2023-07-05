using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using MCStatus;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WatcherBot.Utils;

namespace WatcherBot.Commands;

// ReSharper disable once UnusedType.Global
[Hidden]
public class HyenaCommandModule : BaseCommandModule
{
    private const string Endpoint = "https://api.yeen.land";
    private static readonly HttpClient HttpClient = new();

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly BotMain botMain;
    private readonly string maskPath;
    private readonly MinecraftServer? mcServer;

    public HyenaCommandModule(BotMain bm, IOptions<Config.Config> cfg)
    {
        botMain  = bm;
        maskPath = cfg.Value.YeensayMaskPath;
        mcServer = cfg.Value.MinecraftServers.FirstOrDefault();
    }

    [Command("trash")]
    [Description("garbaggio")]
    public Task Trash(CommandContext context) =>
        throw new Exception("test exception");

    [Command("testmc")]
    [Description("testmc")]
    public async Task TestMc(CommandContext context)
    {
        if (mcServer is not { } server) { return; }

        context.Channel.SendMessageAsync("ack testmc");
        try
        {
            var status =
                await ServerListClient.GetStatusAsync(server.Host, server.Port.GetValueOrDefault(25565));
            var str = string.Format(server.OnlineFormat, status.Description,
                status.Players.Online, status.Players.Max, status.Version.Name);
            context.Channel.SendMessageAsync(str);
        }
        catch (Exception e)
        {
            context.Channel.SendMessageAsync("EXCEPTION: " + e.Message);
        }
    }

    [Command("sus")]
    [Description("smh my head")]
    public async Task Sus(CommandContext context) =>
        await context.RespondDmAsync("https://tenor.com/view/keanu-reeves-knife-gif-19576998");

    [Command("bezos")]
    [Description("ffs")]
    public async Task Bezos(CommandContext context) =>
        await context.RespondDmAsync("please shut the fuck up");

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
        string requestUriString = $"{Endpoint}/id/{id}";
        string reply            = await GetReply(requestUriString);

        await context.RespondAsync(reply);
    }

    [Command("hyenasay")]
    [Aliases("yeensay")]
    [Description("what the yeen say")]
    public async Task HyenaSay(CommandContext context)
    {
        Task<IsModerator> isModeratorTask = botMain.IsUserModerator(context.Member);

        HyenaUrl? url = await QueryApi(Endpoint);

        if (url is null)
        {
            await context.RespondAsync("Something went wrong!");
            return;
        }

        using HttpResponseMessage response = await HttpClient.GetAsync(url.Url);
        await using Stream        stream   = await response.Content.ReadAsStreamAsync();

        using Image image = await Image.LoadAsync(stream);
        using Image mask  = await Image.LoadAsync(maskPath);
        // ReSharper disable once AccessToDisposedClosure
        mask.Mutate(x => x.Resize(image.Size()));
        var options = new GraphicsOptions { AlphaCompositionMode = PixelAlphaCompositionMode.SrcIn };

        // ReSharper disable once AccessToDisposedClosure
        using Image result = mask.Clone(m => m.DrawImage(image, options));

        await using var memoryStream = new MemoryStream();
        await result.SaveAsync(memoryStream, new PngEncoder());
        memoryStream.Position = 0;

        DiscordMessageBuilder builder     = new DiscordMessageBuilder().WithFile("yeensay.png", memoryStream);
        IsModerator           isModerator = await isModeratorTask;

        Task[] tasks =
        {
            context.Message.DeleteAsync(),
            isModerator == IsModerator.Yes || context.Guild != botMain.OutputGuild
                ? context.Channel.SendMessageAsync(builder)
                : Bezos(context),
        };
        await Task.WhenAll(tasks);
    }

    private static async Task<HyenaUrl?> QueryApi(string requestUriString)
    {
        string response = await HttpClient.GetStringAsync(requestUriString);
        var    url      = JsonSerializer.Deserialize<HyenaUrl>(response, JsonSerializerOptions);

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

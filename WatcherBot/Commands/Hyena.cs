using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using WatcherBot.Utils;

namespace WatcherBot.Commands;

// ReSharper disable once UnusedType.Global
[Hidden]
public class HyenaCommandModule : BaseCommandModule
{
    private const string Endpoint = "https://api.yeen.land";
    private const string NtfKey = "%aywb{#2tz+y(h{'Q-cF)&R:UeXIFl3p";
    private static readonly HttpClient HttpClient = new();
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Command("trash")]
    [Description("garbaggio")]
    public Task Trash(CommandContext context) =>
        throw new Exception("test exception");

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

    [Command("Key.bb")]
    [Aliases("key")]
    [Description("next-generation cryptography")]
    public async Task Key(CommandContext context)
    {
        Task[] tasks = { context.Message.DeleteAsync(), context.Channel.SendMessageAsync(NtfKey) };
        await Task.WhenAll(tasks);
    }

    [Command("hyena")]
    [Description("fetch hyena images by id")]
    public async Task Hyena(CommandContext context, [Description("id of hyena photo to fetch")] ulong id)
    {
        string requestUriString = $"{Endpoint}/id/{id}";
        string reply            = await GetReply(requestUriString);

        await context.RespondAsync(reply);
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
        if (await QueryApi(requestUriString) is { } url) { reply = url.Url; }
        else { reply                                             = ":question:"; }

        return reply;
    }

    private record HyenaUrl(string Url);
}

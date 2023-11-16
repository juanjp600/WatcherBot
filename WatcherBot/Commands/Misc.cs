using System;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using WatcherBot.Utils;

namespace WatcherBot.Commands;

// ReSharper disable once UnusedType.Global
[Hidden]
public class MiscCommandModule : BaseCommandModule
{
    public MiscCommandModule() { }

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
}

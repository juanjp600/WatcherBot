using System;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;

namespace WatcherBot.Utils;

[AttributeUsage(AttributeTargets.Method)]
public class RequireOutputGuild : CheckBaseAttribute
{
    public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        await Task.Yield();
        var botMain = (BotMain?)ctx.Services.GetService(typeof(BotMain));
        if (botMain?.DiscordConfig.OutputGuild is null) { return false; }

        return botMain.DiscordConfig.OutputGuild == ctx.Guild;
    }
}

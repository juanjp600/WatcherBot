using System;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;

namespace WatcherBot.Utils;

[AttributeUsage(AttributeTargets.Method)]
public class RequireModeratorRoleInGuild : CheckBaseAttribute
{
    public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        var botMain = (BotMain?)ctx.Services.GetService(typeof(BotMain));
        if (botMain?.OutputGuild is null)
        {
            return false;
        }

        DiscordMember? member = await botMain.OutputGuild.GetMemberAsync(ctx.User.Id);

        return await botMain.IsUserModerator(member).ContinueWith(e => e.Result == IsModerator.Yes);
    }
}

using System;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Serilog;

namespace WatcherBot.Utils;

[AttributeUsage(AttributeTargets.Method)]
public class RequireModeratorRoleInGuild : CheckBaseAttribute
{
    public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        var botMain = (BotMain?)ctx.Services.GetService(typeof(BotMain));
        if (botMain?.OutputGuild is null)
        {
            Log.Logger.Error("Could not find output guild in moderator check");
            return false;
        }

        DiscordMember? member = await botMain.OutputGuild.GetMemberAsync(ctx.User.Id);

        bool result = await botMain.IsUserModerator(member).ContinueWith(e => e.Result == IsModerator.Yes);
        Log.Logger.Debug("IsUserModerator: {Result}", result);
        return result;
    }
}

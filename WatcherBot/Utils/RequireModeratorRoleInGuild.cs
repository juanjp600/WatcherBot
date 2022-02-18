using System;
using System.Linq;
using System.Threading.Tasks;
using DisCatSharp;
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
        if (botMain?.DiscordConfig.OutputGuild is null) { return false; }

        DiscordMember? member = await botMain.DiscordConfig.OutputGuild.GetMemberAsync(ctx.User.Id);
        if (member is null) { return false; }
        if (member.Permissions.HasFlag(Permissions.Administrator)) { return true; }
        return botMain.DiscordConfig.ModeratorRoles.Overlaps(member.Roles);
    }
}

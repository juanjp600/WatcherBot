using System;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;

namespace WatcherBot.Utils;

[AttributeUsage(AttributeTargets.Method)]
public class RequirePermissionInGuild : CheckBaseAttribute
{
    private readonly Permissions permissions;

    public RequirePermissionInGuild(Permissions permissions)
    {
        this.permissions = permissions;
    }

    public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        var botMain = (BotMain?)ctx.Services.GetService(typeof(BotMain));
        if (botMain?.OutputGuild is null)
        {
            return false;
        }

        DiscordMember? member = await botMain.OutputGuild.GetMemberAsync(ctx.User.Id);

        return member?.Permissions.HasFlag(permissions) ?? false;
    }
}

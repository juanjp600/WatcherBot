using System;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using Microsoft.Extensions.Options;
using WatcherBot.Utils;

namespace WatcherBot.Commands;

public class TimeoutCommandModule : BaseCommandModule
{
    private readonly Templates templates;

    public TimeoutCommandModule(IOptions<Config.Config> cfg)
    {
        templates = cfg.Value.Templates;
    }

    private async Task Timeout(CommandContext          context, DiscordMember member, DateTime time,
                               [RemainingText] string? reason,  Anonymous     anon)
    {
        try
        {
            await member.TimeoutAsync(time, reason);
        }
        catch (Exception e)
        {
            await context.Message.Channel
                         .SendMessageAsync($"Error timing out {member.Mention}: {(e.InnerException ?? e).Message}");
            return;
        }

        var unixStr = new DateTimeOffset(time).ToUnixTimeSeconds().ToString();

        string msg = templates.Timeout.Replace("[time]", unixStr)
                              .Replace("[timeouter]",
                                       templates.GetAppealRecipients(context.User.UsernameWithDiscriminator, anon))
                              .Replace("[reason]", reason ?? "No reason provided");

        var appeal = $"The appeal message sent was:\n{msg}";
        try
        {
            await member.SendMessageAsync(msg);
        }
        catch
        {
            appeal = "The appeal message could not be sent.";
        }

        await context.Message.Channel.SendMessageAsync($"Timed out {member.Mention} until <t:{unixStr}>. {appeal}");
    }

    [Command("timeout")]
    [Description("Timeout a member and send them an appeal message via DMs, including your username for contact.")]
    [RequirePermissionInGuild(Permissions.ModerateMembers)]
    [RequireModeratorRoleInGuild]
    [RequireDmOrOutputGuild]
    public async Task Timeout(CommandContext          context, DiscordMember member, DateTime time,
                              [RemainingText] string? reason = null) =>
        await Timeout(context, member, time, reason, Anonymous.No);

    [Command("timeout_anon")]
    [Description("Timeout a member and send them an appeal message via DMs with a default username for contact.")]
    [RequirePermissionInGuild(Permissions.ModerateMembers)]
    [RequireModeratorRoleInGuild]
    [RequireDmOrOutputGuild]
    public async Task TimeoutAnon(CommandContext          context, DiscordMember member, DateTime time,
                                  [RemainingText] string? reason = null) =>
        await Timeout(context, member, time, reason, Anonymous.Yes);
}

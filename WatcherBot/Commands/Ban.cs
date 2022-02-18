using System;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Microsoft.Extensions.Logging;
using WatcherBot.Config;
using WatcherBot.Utils;

namespace WatcherBot.Commands;

// ReSharper disable once UnusedType.Global
public class BanCommandModule : BaseCommandModule
{
    private readonly BotMain botMain;

    public BanCommandModule(BotMain bm) => botMain = bm;

    private Templates Templates => botMain.Config.Templates;

    private async Task BanMember(
        CommandContext context,
        DiscordMember  member,
        string?        reason,
        Anonymous      anon = Anonymous.No)
    {
        context.Client.Logger.LogInformation($"Banning {member.Mention} with reason {reason}");

        try { await botMain.DiscordConfig.OutputGuild.BanMemberAsync(member, reason: reason); }
        catch (Exception e)
        {
            context.Client.Logger.LogError($"Error banning {member.Mention}: {e}");
            await context.Message.Channel
                .SendMessageAsync($"Error banning {member.UsernameWithDiscriminator}: {(e.InnerException ?? e).Message}");
            return;
        }

        string banMsg = Templates.Ban.Replace("[reason]", reason ?? "No reason provided")
                                     .Replace("[banner]", Templates.GetAppealRecipients(context.User.UsernameWithDiscriminator, anon));

        string appeal = "The appeal message could not be sent.";
        try
        {
            await member.SendMessageAsync(banMsg);
            appeal = $"The message sent was the following:\n{banMsg}";
            context.Client.Logger.LogDebug($"Sent banned user {member.Mention} DM");
        }
        catch { context.Client.Logger.LogWarning("Failed to send DM to banned user"); }
        await context.Message.Channel.SendMessageAsync($"{member.Mention} has been banned. {appeal}");
    }

    [Command("ban")]
    [Description("Ban a member and send them an appeal message via DMs, including your username for contact.")]
    [RequirePermissionInGuild(Permissions.BanMembers)]
    [RequireModeratorRoleInGuild]
    [RequireDmOrOutputGuild]
    public async Task Ban(CommandContext context, DiscordMember member, [RemainingText] string? reason = null) =>
        await BanMember(context, member, reason);

    [Command("ban_anon")]
    [Description("Ban a member and send them an appeal message via DMs with a default username for contact.")]
    [RequirePermissionInGuild(Permissions.BanMembers)]
    [RequireModeratorRoleInGuild]
    [RequireDmOrOutputGuild]
    public async Task BanAnon(CommandContext context, DiscordMember member, [RemainingText] string? reason = null) =>
        await BanMember(context, member, reason, Anonymous.Yes);
}

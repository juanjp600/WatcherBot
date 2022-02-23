using System;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WatcherBot.Config;
using WatcherBot.Utils;

namespace WatcherBot.Commands;

// ReSharper disable once UnusedType.Global
public class BanCommandModule : BaseCommandModule
{
    private readonly BotMain botMain;
    private readonly Config.Config config;

    public BanCommandModule(BotMain bm, IOptions<Config.Config> cfg)
    {
        botMain = bm;
        config  = cfg.Value;
    }

    private BanTemplate BanTemplate => config.BanTemplate;

    private async Task BanMember(
        CommandContext context,
        DiscordMember  member,
        string?        reason,
        Anonymous      anon = Anonymous.No)
    {
        DiscordUser banner = context.User;
        // The commands that invoke BanMember should not be executed if the caller is not a moderator
        // but this check is here just in case.
        if (await botMain.IsUserModerator(banner) == IsModerator.No)
        {
            await context.RespondAsync($"Error executing !ban: {banner.Mention} is not a moderator");
            return;
        }

        context.Client.Logger.LogInformation("Banning {Banee} with reason {Reason}",
                                             member.UsernameWithDiscriminator,
                                             reason);
        // Send the banned user a DM with the reason and appeal information.
        DiscordDmChannel baneeDm = await member.CreateDmChannelAsync();

        string bannerStr;
        if (anon == Anonymous.Yes) { bannerStr = BanTemplate.DefaultAppeal; }
        else
        {
            // This is a username#discriminator to contact by default
            bannerStr = banner.UsernameWithDiscriminator;
            // If it's the same person we don't need to say Foobar#1234 or Foobar#1234
            // so catch that here.
            bannerStr = bannerStr != BanTemplate.DefaultAppeal
                            ? $"`{bannerStr}` or `{BanTemplate.DefaultAppeal}`"
                            : $"`{bannerStr}`";
        }

        string banMsg = BanTemplate.Template.Replace("[reason]", reason ?? "No reason provided")
                                   .Replace("[banner]", bannerStr);

        var feedback = $"{member.Mention} has been banned. The appeal message could not be sent.\n";
        try
        {
            DiscordMessage directMsg = await baneeDm.SendMessageAsync(banMsg);
            if (directMsg is not null && directMsg.Id != 0)
            {
                feedback = $"{member.Mention} has been banned. The message sent was the following:\n{banMsg}";
                context.Client.Logger.LogDebug("Sent banned user {Banee} DM", member.UsernameWithDiscriminator);
            }
        }
        catch { context.Client.Logger.LogWarning("Failed to send DM to banned user"); }

        await context.Message.Channel.SendMessageAsync(feedback);
        try { await botMain.OutputGuild.BanMemberAsync(member, reason: reason); }
        catch (Exception e)
        {
            context.Client.Logger.LogError(e, "Error banning {Banee}", member.UsernameWithDiscriminator);
            await
                context.Message.Channel
                       .SendMessageAsync($"Error banning {member.UsernameWithDiscriminator}: {(e.InnerException ?? e).Message}");
        }
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

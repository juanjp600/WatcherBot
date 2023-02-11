using System;
using System.Linq;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using WatcherBot.Utils;

namespace WatcherBot.Commands;

// ReSharper disable once UnusedType.Global
public class AdminCommandModule : BaseCommandModule
{
    [Command("whois")]
    [Description("Display information about a member or user.")]
    [RequireModeratorRoleInGuild]
    [RequireDmOrOutputGuild]
    public async Task WhoIs(CommandContext context, [Description("The member to look up")] DiscordMember member)
    {
        DiscordEmbedBuilder embed =
            new DiscordEmbedBuilder().WithColor(member.Color)
                                     .WithAuthor(member.UsernameWithDiscriminator,
                                                 iconUrl: member.AvatarUrl)
                                     .WithThumbnail(member.GuildAvatarUrl)
                                     .WithTimestamp(DateTimeOffset.Now)
                                     .WithFooter($"ID: {member.Id}")
                                     .WithDescription(member.Mention)
                                     .AddField(new DiscordEmbedField("Joined",
                                                                     Formatter.Timestamp(member.JoinedAt,
                                                                         TimestampFormat.ShortDateTime),
                                                                     true))
                                     .AddField(new DiscordEmbedField("Created",
                                                                     Formatter.Timestamp(member.CreationTimestamp,
                                                                         TimestampFormat.ShortDateTime),
                                                                     true));
        if (member.Roles.Any())
        {
            embed.AddField(new DiscordEmbedField($"Roles ({member.Roles.Count})",
                                                 string.Join(", ", member.Roles.Select(r => r.Name))));
        }

        await context.RespondAsync(embed.Build());
    }

    [Command("whois")]
    [Description("Display information about a member or user.")]
    [RequireModeratorRoleInGuild]
    [RequireDmOrOutputGuild]
    public async Task WhoIs(CommandContext context, [Description("The user to look up")] DiscordUser user)
    {
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                                    .WithAuthor(user.UsernameWithDiscriminator, iconUrl: user.AvatarUrl)
                                    .WithThumbnail(user.AvatarUrl)
                                    .WithTimestamp(DateTimeOffset.Now)
                                    .WithFooter(user.Id.ToString())
                                    .WithDescription($"{user.Mention}\nNot in server")
                                    .AddField(new DiscordEmbedField("Created",
                                                                    Formatter.Timestamp(user.CreationTimestamp,
                                                                        TimestampFormat.ShortDateTime),
                                                                    true));

        await context.RespondAsync(embed.Build());
    }
}

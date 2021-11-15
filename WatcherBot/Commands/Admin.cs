using System;
using System.Linq;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using WatcherBot.Utils;

namespace WatcherBot.Commands
{
    // ReSharper disable once UnusedType.Global
    public class AdminCommandModule : BaseCommandModule
    {
        [Command("whois")]
        [Description("Display information about a member or user.")]
        [RequireModeratorRoleInGuild]
        [RequireDmOrOutputGuild]
        public async Task WhoIs(
            CommandContext context,
            [Description("The member to look up")] DiscordMember member)
        {
            DiscordEmbed embed = new DiscordEmbedBuilder()
                                 .WithColor(member.Color)
                                 .WithAuthor(name: member.UsernameWithDiscriminator, iconUrl: member.AvatarUrl)
                                 .WithThumbnail(member.GuildAvatarUrl)
                                 .WithTimestamp(DateTimeOffset.Now)
                                 .WithFooter($"ID: {member.Id}")
                                 .WithDescription(member.Mention)
                                 .AddField("Joined", Formatter.Timestamp(member.JoinedAt, TimestampFormat.ShortDateTime), inline: true)
                                 .AddField("Created", Formatter.Timestamp(member.CreationTimestamp, TimestampFormat.ShortDateTime), inline: true)
                                 .AddField($"Roles ({member.Roles.Count()})",
                                           string.Join(", ", member.Roles.Select(r => r.Name)))
                                 .Build();
            await context.RespondAsync(embed);
        }

        [Command("whois")]
        [Description("Display information about a member or user.")]
        [RequireModeratorRoleInGuild]
        [RequireDmOrOutputGuild]
        public async Task WhoIs(
            CommandContext context,
            [Description("The user to look up")] DiscordUser user)
        {
            DiscordEmbed embed = new DiscordEmbedBuilder()
                                 .WithAuthor(name: user.UsernameWithDiscriminator, iconUrl: user.AvatarUrl)
                                 .WithThumbnail(user.AvatarUrl)
                                 .WithTimestamp(DateTimeOffset.Now)
                                 .WithFooter(user.Id.ToString())
                                 .WithDescription($"{user.Mention}\nNot in server")
                                 .AddField("Created", Formatter.Timestamp(user.CreationTimestamp, TimestampFormat.ShortDateTime), inline: true)
                                 .Build();
            await context.RespondAsync(embed);
        }
    }
}

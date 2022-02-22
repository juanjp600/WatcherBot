using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using WatcherBot.Utils;
using System;

namespace WatcherBot.Logging;

[EventHandler]
public class DiscordLogger
{
    private readonly BotMain botMain;

    public DiscordLogger(BotMain bm) {
        botMain = bm;
    }

    private DiscordChannel? getChannelForGuild(DiscordGuild guild)
    {
        ulong logChannelId;
        if (!botMain.Config.GuildToLogChannel.TryGetValue(guild.Id, out logChannelId)) { return null; }
        return guild.GetChannel(logChannelId);
    }

    [Event]
    public async Task MessageDeleted(DiscordClient client, MessageDeleteEventArgs args)
    {
        DiscordChannel? logChannel = getChannelForGuild(args.Guild);
        if (logChannel is null) { return; }

        DiscordMessage message = args.Message;
        DiscordUser author = message.Author;
        if (author is null) { return; }

        List<DiscordEmbed> embeds = new();
        embeds.Add(new DiscordEmbedBuilder()
            .WithAuthor(author.UsernameWithDiscriminator, iconUrl: author.AvatarUrl)
            .WithColor(DiscordColor.Orange)
            .WithImageUrl(author.ProfileUrl)
            .WithDescription($"**Message by {author.Mention} deleted in {message.Channel.Mention}:**\n{message.Content}")
            .WithTimestamp(message.Timestamp)
            .WithFooter($"ID: {message.Id}"));

        DiscordEmbedBuilder basicAttachmentEmbed(DiscordAttachment a, int i, string extra = "")
            => new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Orange)
                .WithDescription($"**Attachment #{i}:**\n`{a.FileName}`\n{extra}");

        embeds.AddRange(message.Attachments.Select((a, i)
            => a.MediaType.StartsWith("image/")
                ? basicAttachmentEmbed(a, i + 1)
                    .WithImageUrl(a.Url).Build()
                : basicAttachmentEmbed(a, i + 1, a.Url)
        ));

        // Videos don't re-embed
        embeds.AddRange(message.Embeds.Where(e => e.Type != "video"));

        // Only 10 embeds per message
        await Task.WhenAll(embeds.Chunk(10).Select(g => logChannel.SendMessageAsync(
                new DiscordMessageBuilder().AddEmbeds(g))));
    }

    private DiscordEmbedBuilder getBasicInfo(DiscordMember member)
        => new DiscordEmbedBuilder()
            .WithThumbnail(member.AvatarUrl)
            .WithDescription(member.Mention + " " + member.UsernameWithDiscriminator);

    [Event]
    public async Task GuildMemberAdded(DiscordClient client, GuildMemberAddEventArgs args)
    {
        DiscordChannel? logChannel = getChannelForGuild(args.Guild);
        if (logChannel is null) { return; }

        DiscordMember member = args.Member;

        await logChannel.SendMessageAsync(getBasicInfo(member)
            .WithColor(DiscordColor.SapGreen)
            .WithAuthor("Member joined")
            .AddField("Account Created", $"<t:{member.CreationTimestamp.ToUnixTimeSeconds()}:R>")
        );
    }

    [Event]
    public async Task GuildMemberRemoved(DiscordClient client, GuildMemberRemoveEventArgs args)
    {
        DiscordChannel? logChannel = getChannelForGuild(args.Guild);
        if (logChannel is null) { return; }

        DiscordMember member = args.Member;

        await logChannel.SendMessageAsync(getBasicInfo(member)
            .WithColor(DiscordColor.Red)
            .WithAuthor("Member left")
            .AddField("Joined", $"<t:{member.JoinedAt.ToUnixTimeSeconds()}:R>")
        );
    }
}


using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WatcherBot.Models;

namespace WatcherBot.Utils;

public class MessageDeleters : IDisposable
{
    public enum MessageDeletionReason
    {
        DisallowedInvite,
        ViolateAttachmentLimits,
        ProhibitedFormatting,
        CringeMessage,
        PotentialSpam,
        BadWord,
        ReplyInNoConversationChannel,
    }

    private readonly BotMain botMain;
    private readonly Config.Config config;
    private readonly ILogger logger;

    public MessageDeleters(BotMain botMain, IOptions<Config.Config> cfg)
    {
        this.botMain = botMain;
        logger       = botMain.Client.Logger;
        config       = cfg.Value;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private static bool GeneralCondition(DiscordMessage msg) => !(msg.Author.IsBot || msg.Channel is DiscordDmChannel);

    public Task ContainsDisallowedInvite(DiscordClient sender, MessageCreateEventArgs args)
    {
        Delete DeletionCondition()
        {
            var thread = botMain.OutputGuild.GetThread(args.Channel.Id);

            if (config.InvitesAllowedOnChannels.Contains(args.Message.Channel.Id)
                || (thread is { Parent.Id: var parentId } && config.InvitesAllowedOnChannels.Contains(parentId)))
            {
                return Delete.No;
            }

            if (args.Guild is null || config.InvitesAllowedOnServers.Contains(args.Guild.Id))
            {
                return Delete.No;
            }

            return TextContainsInvite(args.Message.Content) ? Delete.Yes : Delete.No;
        }

        if (!GeneralCondition(args.Message) || DeletionCondition() != Delete.Yes)
        {
            return Task.CompletedTask;
        }

        logger.LogInformation("Deleting message sent by {User} for reason {Reason}",
                              args.Message.Author.UsernameWithDiscriminator,
                              MessageDeletionReason.DisallowedInvite);
        return DeleteMsg(args.Message);
    }

    public static bool TextContainsInvite(string messageContent)
    {
        string[] invites = { "discord.gg/", "discord.com/invite", "discordapp.com/invite" };
        return invites.Any(i => messageContent.Contains(i, StringComparison.OrdinalIgnoreCase));
    }

    public Task MessageWithinAttachmentLimits(DiscordClient sender, MessageCreateEventArgs args)
    {
        Delete DeletionCondition()
        {
            if (!config.AttachmentLimits.ContainsKey(args.Channel.Id))
            {
                return Delete.No;
            }

            bool insecureLink = args.Message.Content.Contains("http://", StringComparison.OrdinalIgnoreCase);
            int numberWellSizedAttachments =
                args.Message.Attachments.Count(a => a.Width is null
                                                    || a.Height is null
                                                    || a.Width >= 16 && a.Height >= 16);
            int numberLinks = args.Message.Content.CountSubstrings("https://");
            int sum         = numberWellSizedAttachments + numberLinks;

            bool attachmentCountWithinLimits = config.AttachmentLimits[args.Channel.Id].Contains(sum);
            bool allAttachmentsWellSized     = numberWellSizedAttachments == args.Message.Attachments.Count;

            return attachmentCountWithinLimits && allAttachmentsWellSized && !insecureLink ? Delete.No : Delete.Yes;
        }

        if (!GeneralCondition(args.Message) || DeletionCondition() != Delete.Yes)
        {
            return Task.CompletedTask;
        }

        logger.LogInformation("Deleting message sent by {User} for reason {Reason}",
                              args.Message.Author.UsernameWithDiscriminator,
                              MessageDeletionReason.ViolateAttachmentLimits);
        return DeleteMsg(args.Message);
    }

    public Task ProhibitFormattingFromUsers(DiscordClient sender, MessageCreateEventArgs args)
    {
        if (!GeneralCondition(args.Message)
            || !config.ProhibitFormattingFromUsers.Contains(args.Author.Id)
            || !config.FormattingCharacters.Overlaps(args.Message.Content))
        {
            return Task.CompletedTask;
        }

        logger.LogInformation("Deleting message sent by {User} for reason {Reason}",
                              args.Message.Author.UsernameWithDiscriminator,
                              MessageDeletionReason.ProhibitedFormatting);
        return DeleteMsg(args.Message);
    }

    public Task DeleteCringeMessages(DiscordClient sender, MessageCreateEventArgs args)
    {
        Task _ = Task.Run(async () =>
        {
            IsCringe UserIsCringe()
            {
                using var databaseContext = new WatcherDatabaseContext();
                var       user            = User.GetOrCreateUser(databaseContext, args.Message.Author.Id);
                IsCringe channelIsCringe = config.CringeChannels.Contains(args.Message.Channel.Id)
                                               ? IsCringe.Yes
                                               : IsCringe.No;
                if (args.Message.Channel is not DiscordDmChannel)
                {
                    user.NewMessage(channelIsCringe);
                    try
                    {
                        databaseContext.SaveChanges();
                    }
                    catch (DbUpdateException exc)
                    {
                        Console.WriteLine($"{nameof(databaseContext.SaveChanges)} threw an exception:");
                        Console.WriteLine($"{exc.InnerException?.Message ?? exc.Message}");
                        Console.WriteLine($"{exc.InnerException?.StackTrace ?? exc.StackTrace}");
                    }
                }

                // it's cringe to bool to cringe
                return (channelIsCringe.ToBool() && user.IsCringe.ToBool()).ToCringe();
            }

            if (GeneralCondition(args.Message) && UserIsCringe() == IsCringe.Yes)
            {
                logger.LogInformation("Deleting message sent by {User} for reason {Reason}",
                                      args.Message.Author.UsernameWithDiscriminator,
                                      MessageDeletionReason.CringeMessage);
                await DeleteMsg(args.Message);
            }
        });
        return Task.CompletedTask;
    }

    public Task DeleteBadWords(DiscordClient sender, MessageCreateEventArgs args)
    {
        Task _ = Task.Run(async () =>
        {
            if (!args.Channel.IsPrivate && botMain.OutputGuild != args.Guild)
            {
                return;
            }

            if (args.Author.IsBot)
            {
                return;
            }

            if (await botMain.IsUserModerator(args.Author) == IsModerator.Yes)
            {
                return;
            }

            if (await botMain.IsUserExemptFromSpamFilter(args.Author) == IsExemptFromSpamFilter.Yes)
            {
                return;
            }

            string messageContent       = args.Message.Content;
            string messageContentToTest = messageContent.ToLowerInvariant();

            var hits = config.BadSubstrings.Select(bw =>
            {
                var result = LevenshteinDistance.FindSubstr(messageContentToTest, bw.Substring, bw.MaxDistance);
                var substr = result is { Index: var index, Length: var length }
                    ? messageContent.Substring(index, length)
                    : "";
                return (Found: substr, Matched: bw.Substring);
            }).Where(sp => !string.IsNullOrWhiteSpace(sp.Found)).ToArray();
            if (hits.Length > 0)
            {
                logger.LogInformation("Deleting message sent by and muting {User} for reason {Reason}",
                    args.Message.Author.UsernameWithDiscriminator,
                    MessageDeletionReason.BadWord);
                var reason = $"*Hits*: {string.Join(", ", hits.Select(h => $"{h.Found} (matched \"{h.Matched}\")"))}";

                _ = BarotraumaToolBox.ReportSpam(botMain, args.Message, reason, badWords: true);

                _ = botMain.MuteUser(args.Author, $"Bad words {DateTime.UtcNow}");
                _ = args.Message.DeleteAsync();
            }
        });

        return Task.CompletedTask;
    }

    public Task DeletePotentialSpam(DiscordClient sender, MessageCreateEventArgs args)
    {
        Task _ = Task.Run(async () =>
        {
            if (!args.Channel.IsPrivate && botMain.OutputGuild != args.Guild)
            {
                return;
            }

            if (args.Author.IsBot)
            {
                return;
            }

            if (await botMain.IsUserModerator(args.Author) == IsModerator.Yes)
            {
                return;
            }

            if (await botMain.IsUserExemptFromSpamFilter(args.Author) == IsExemptFromSpamFilter.Yes)
            {
                return;
            }

            if (config.InvitesAllowedOnChannels.Contains(args.Channel.Id)
                && TextContainsInvite(args.Message.Content))
            {
                return;
            }

            //if (args.Channel.Id != config.SpamReportChannel) { return; }

            string messageContent       = args.Message.Content;
            string messageContentToTest = messageContent.ToLowerInvariant();
            foreach (string safeSubstr in config.KnownSafeSubstrings)
            {
                messageContentToTest = messageContentToTest.Replace(safeSubstr, "");
            }

            if (!messageContentToTest.ContainsLink())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(messageContentToTest))
            {
                logger.LogInformation("Message from {Sender} is blank!", args.Author.UsernameWithDiscriminator);
                return;
            }

            var hits = config.GetSpamFilterHits(messageContentToTest);

            if (hits.Sum(h => h.Weight) >= 2)
            {
                logger.LogInformation("Deleting message sent by and muting {User} for reason {Reason}",
                                      args.Message.Author.UsernameWithDiscriminator,
                                      MessageDeletionReason.PotentialSpam);
                var reason = $"*Hits*: {string.Join(", ", hits.Select(h => $"{h.InText} (matched \"{h.InFilter}\")"))}";

                _ = BarotraumaToolBox.ReportSpam(botMain, args.Message, reason, badWords: false);

                _ = botMain.MuteUser(args.Author, $"Potential spam {DateTime.UtcNow}");
                _ = args.Message.DeleteAsync();
            }
        });

        return Task.CompletedTask;
    }

    public Task ReplyInNoConversationChannel(DiscordClient sender, MessageCreateEventArgs args)
    {
        if (!config.NoReplies.Contains(args.Channel.Id) || args.Message.ReferencedMessage is null)
        {
            return Task.CompletedTask;
        }

        logger.LogInformation("Deleting message sent by {User} for reason {Reason}",
                              args.Message.Author.UsernameWithDiscriminator,
                              MessageDeletionReason.ReplyInNoConversationChannel);
        return DeleteMsg(args.Message);
    }

    private Task DeleteMsg(DiscordMessage msg)
    {
        async Task Delete(Task<IsModerator> t)
        {
            if (t.Result == IsModerator.No)
            {
                await msg.DeleteAsync();
            }
        }

        return botMain.IsUserModerator(msg.Author).ContinueWith(Delete);
    }

    private enum Delete
    {
        No,
        Yes,
    }
}

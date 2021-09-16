using System;
using System.Linq;
using System.Threading.Tasks;
using Bot600.Models;
using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using Microsoft.EntityFrameworkCore;

namespace Bot600.Utils
{
    public class MessageDeleters : IDisposable
    {
        private readonly BotMain botMain;
        private readonly WatcherDatabaseContext databaseContext;

        private enum Delete
        {
            No,
            Yes
        }

        public MessageDeleters(BotMain botMain)
        {
            this.botMain    = botMain;
            databaseContext = new WatcherDatabaseContext();
        }

        public void Dispose()
        {
            databaseContext.Dispose();
            GC.SuppressFinalize(this);
        }

        private bool GeneralCondition(DiscordMessage msg) => !(msg.Author.IsBot || msg.Channel is DiscordDmChannel);

        public Task ContainsDisallowedInvite(DiscordClient sender, MessageCreateEventArgs args)
        {
            Delete DeletionCondition()
            {
                if (botMain.Config.InvitesAllowedOnChannels.Contains(args.Message.Channel.Id))
                {
                    return Delete.No;
                }

                if (args.Guild is null || botMain.Config.InvitesAllowedOnServers.Contains(args.Guild.Id))
                {
                    return Delete.No;
                }

                string[] invites = { "discord.gg/", "discord.com/invite", "discordapp.com/invite" };
                return invites.Any(i => args.Message.Content.Contains(i, StringComparison.OrdinalIgnoreCase)) ? Delete.Yes : Delete.No;
            }

            return GeneralCondition(args.Message) && DeletionCondition() == Delete.Yes ? DeleteMsg(args.Message) : Task.CompletedTask;
        }

        public Task MessageWithinAttachmentLimits(DiscordClient sender, MessageCreateEventArgs args)
        {
            Delete DeletionCondition()
            {
                if (!botMain.Config.AttachmentLimits.ContainsKey(args.Channel.Id)) { return Delete.No; }

                bool insecureLink = args.Message.Content.Contains("http://", StringComparison.OrdinalIgnoreCase);
                int numberWellSizedAttachments = args.Message.Attachments.Count(a => a.Width is null
                    || a.Height is null
                    || a.Width >= 16 && a.Height >= 16);
                int numberLinks = args.Message.Content.CountSubstrings("https://");
                int sum = numberWellSizedAttachments + numberLinks;

                bool attachmentCountWithinLimits = botMain.Config.AttachmentLimits[args.Channel.Id].Contains(sum);
                bool allAttachmentsWellSized = numberWellSizedAttachments == args.Message.Attachments.Count;

                return (attachmentCountWithinLimits && allAttachmentsWellSized && !insecureLink) ? Delete.No : Delete.Yes;
            }

            return GeneralCondition(args.Message) && DeletionCondition() == Delete.Yes ? DeleteMsg(args.Message) : Task.CompletedTask;
        }

        public Task ProhibitFormattingFromUsers(DiscordClient sender, MessageCreateEventArgs args) =>
            GeneralCondition(args.Message)
            && botMain.Config.ProhibitFormattingFromUsers.Contains(args.Author.Id)
            && botMain.Config.FormattingCharacters.Overlaps(args.Message.Content)
                ? DeleteMsg(args.Message)
                : Task.CompletedTask;

        public Task DeleteCringeMessages(DiscordClient sender, MessageCreateEventArgs args)
        {
            Task _ = Task.Run(async () =>
            {
                IsCringe UserIsCringe()
                {
                    User user = User.GetOrCreateUser(databaseContext, args.Message.Author.Id);
                    IsCringe channelIsCringe = botMain.Config.CringeChannels.Contains(args.Message.Channel.Id)
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
                    await DeleteMsg(args.Message);
                }
            });
            return Task.CompletedTask;
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
    }
}

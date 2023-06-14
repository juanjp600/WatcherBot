using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WatcherBot.Utils;

public class DuplicateMessageFilter : LoopingTask
{
    private const int MaxDuplicateMessages = 3;
    private static readonly TimeSpan KeepDuration = TimeSpan.FromSeconds(60);
    private readonly ConcurrentDictionary<DiscordUser, ConcurrentQueue<DiscordMessage>> cache = new();

    public DuplicateMessageFilter(BotMain botMain, IOptions<Config.Config> config) : base(botMain, config)
    { }

    protected override TimeSpan LoopFrequency => TimeSpan.FromSeconds(1);

    public override void Dispose()
    {
        base.Dispose();
        cache.Clear();
        GC.SuppressFinalize(this);
    }

    protected override async Task LoopWork()
    {
        DateTimeOffset currentTime = DateTimeOffset.Now;
        foreach ((DiscordUser user, ConcurrentQueue<DiscordMessage> messages) in cache)
        {
            try
            {
                // Remove old messages
                while (messages.TryPeek(out DiscordMessage? message)
                       && currentTime - message.Timestamp >= KeepDuration)
                {
                    Logger.LogInformation("Removing old message for {User}: {CurrentTime} - {MostRecentMessage} = {TimeDifference}",
                                          user.UsernameWithDiscriminator,
                                          currentTime,
                                          message.Timestamp,
                                          currentTime - message.Timestamp);
                    messages.TryDequeue(out message);
                }

                // Skip empty queues
                if (messages.IsEmpty) { continue; }

                IGrouping<string, DiscordMessage>? duplicateMessages;
                int                                numberDuplicates;

                // Now check if any of the remaining messages are identical
                (duplicateMessages, numberDuplicates) = messages.GroupBy(m => m.Content)
                                                                .Select(g => (Messages: g, Count: g.Count()))
                                                                .MaxBy(t => t.Count);

                if (numberDuplicates < MaxDuplicateMessages) { continue; }

                Logger.LogInformation("Deleting messages sent by and muting {User} for reason {Reason} (sent {Count} messages with content {Content})",
                                      user.UsernameWithDiscriminator,
                                      MessageDeleters.MessageDeletionReason.PotentialSpam,
                                      numberDuplicates,
                                      duplicateMessages.First().Content);

                DiscordChannel[] channels = duplicateMessages.Select(m => m.Channel).DistinctBy(c => c.Id).ToArray();

                string reason =
                    $"{numberDuplicates} copies of this message sent in the last {KeepDuration.TotalSeconds}s"
                    + $" in {string.Join(", ", channels.Select(c => c.Mention))}.";
                await BarotraumaToolBox.ReportSpam(BotMain, duplicateMessages.First(), reason, badWords: false);

                await BotMain.MuteUser(user, "Auto-detected spam messages");
                await Task.WhenAll(duplicateMessages.Select(m => m.DeleteAsync("Auto-detected spam message")));

                // Clear the queue
                messages.Clear();
            }
            catch (Exception e)
            {
                messages.TryPeek(out DiscordMessage? sus);
                Logger.LogError(e,
                                "Duplicate filter failed for user {User}",
                                sus?.Author?.UsernameWithDiscriminator ?? "[NULL]");
                messages.Clear();
            }
        }
    }

    private Func<DiscordUser, ConcurrentQueue<DiscordMessage>> Add(DiscordMessage message) =>
        user =>
        {
            ConcurrentQueue<DiscordMessage> queue = new();
            return Update(message)(user, queue);
        };

    private Func<DiscordUser, ConcurrentQueue<DiscordMessage>, ConcurrentQueue<DiscordMessage>> Update(
        DiscordMessage message) =>
        (_, current) =>
        {
            if (message.Channel.GuildId == Config.OutputGuildId
                && !string.IsNullOrWhiteSpace(message.Content))
            {
                bool hasUrl = message.Content.ContainsLink();
                bool hasSpamFilterHits = Config.GetSpamFilterHits(message.Content.ToLowerInvariant()).Length > 0;
                if (hasUrl || hasSpamFilterHits)
                {
                    current.Enqueue(message);
                }

                // Don't let the queue grow too much
                while (current.Count > 25) { current.TryDequeue(out var __); }
            }

            return current;
        };

    public Task MessageCreated(DiscordClient sender, MessageCreateEventArgs args)
    {
        DateTimeOffset timestamp = args.Message.Timestamp;
        DateTimeOffset now       = DateTimeOffset.Now;
        Logger.LogInformation("Message created at timestamp: {Timestamp}; Now: {Now}", timestamp, now);
        cache.AddOrUpdate(args.Message.Author, Add(args.Message), Update(args.Message));
        return Task.CompletedTask;
    }
}

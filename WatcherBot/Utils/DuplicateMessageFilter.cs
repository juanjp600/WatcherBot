using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using Microsoft.Extensions.Logging;

namespace WatcherBot.Utils;

public class DuplicateMessageFilter : IDisposable
{
    private const int MaxDuplicateMessages = 3;
    private static readonly TimeSpan LoopFrequency = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan KeepDuration = TimeSpan.FromSeconds(5);
    private readonly BotMain botMain;
    private readonly ILogger logger;
    private readonly ConcurrentDictionary<DiscordUser, ConcurrentQueue<DiscordMessage>> cache;
    public readonly CancellationTokenSource CancellationTokenSource;

    public DuplicateMessageFilter(BotMain botMain)
    {
        CancellationTokenSource = new CancellationTokenSource();
        cache                   = new ConcurrentDictionary<DiscordUser, ConcurrentQueue<DiscordMessage>>();
        this.botMain            = botMain;
        logger                  = botMain.Client.Logger;
    }

    public Task? Loop { get; private set; }

    public void Dispose()
    {
        Cancel();
        cache.Clear();
        Loop?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Start()
    {
        if (CancellationTokenSource.IsCancellationRequested) return;
        Loop = Task.Run(MainLoop);
    }

    public void Cancel() => CancellationTokenSource.Cancel();

    private async Task MainLoop()
    {
        while (!CancellationTokenSource.IsCancellationRequested)
        {
            DateTimeOffset currentTime = DateTimeOffset.Now;
            foreach ((DiscordUser user, ConcurrentQueue<DiscordMessage> messages) in cache)
            {
                // Skip empty queues
                if (messages.IsEmpty) continue;

                IGrouping<int, DiscordMessage>? duplicateMessages;
                int numberDuplicates;
                lock (messages)
                {
                    // Remove old messages
                    while (messages.TryPeek(out DiscordMessage? message)
                           && currentTime - message.Timestamp >= KeepDuration)
                        messages.TryDequeue(out message);

                    // Now check if any of the remaining messages are identical
                    (duplicateMessages, numberDuplicates) = messages.GroupBy(m => m.Content.GetHashCode())
                                                                    .Select(g => (Messages: g, Count: g.Count()))
                                                                    .MaxBy(t => t.Count);
                }


                if (numberDuplicates < MaxDuplicateMessages) continue;

                logger.LogInformation(
                    "Deleting messages sent by and muting {User} for reason {Reason} (sent {Count} messages with content {Content})",
                    user.UsernameWithDiscriminator,
                    MessageDeleters.MessageDeletionReason.PotentialSpam,
                    numberDuplicates,
                    duplicateMessages.First().Content);

                await botMain.MuteUser(user, "Auto-detected spam messages");
                await Task.WhenAll(duplicateMessages.Select(m => m.DeleteAsync("Auto-detected spam message")));

                string reason =
                    $"{numberDuplicates} copies of this message sent in the last {KeepDuration.TotalSeconds}s in {string.Join(", ", duplicateMessages.Select(m => m.Channel).Distinct().Select(c => c.Mention))}.";
                await BarotraumaToolBox.ReportSpam(botMain, duplicateMessages.First(), reason);
            }

            await Task.Delay(LoopFrequency);
        }
    }

    private static Func<DiscordUser, ConcurrentQueue<DiscordMessage>> Add(DiscordMessage message) =>
        _ =>
        {
            ConcurrentQueue<DiscordMessage> queue = new();
            queue.Enqueue(message);
            return queue;
        };

    private static Func<DiscordUser, ConcurrentQueue<DiscordMessage>, ConcurrentQueue<DiscordMessage>> Update(
        DiscordMessage message) =>
        (_, current) =>
        {
            current.Enqueue(message);
            return current;
        };

    public Task MessageCreated(DiscordClient sender, MessageCreateEventArgs args)
    {
        cache.AddOrUpdate(args.Message.Author, Add(args.Message), Update(args.Message));
        return Task.CompletedTask;
    }
}

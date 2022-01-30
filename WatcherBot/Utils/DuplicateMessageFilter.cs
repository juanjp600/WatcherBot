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
    private static readonly TimeSpan KeepDuration = TimeSpan.FromSeconds(60);
    private readonly BotMain botMain;
    private readonly ConcurrentDictionary<DiscordUser, ConcurrentQueue<DiscordMessage>> cache;
    public readonly CancellationTokenSource CancellationTokenSource;
    private readonly ILogger logger;

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
        if (CancellationTokenSource.IsCancellationRequested) { return; }

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
                try
                {
                    // Remove old messages
                    while (messages.TryPeek(out DiscordMessage? message)
                           && currentTime - message.Timestamp >= KeepDuration)
                    {
                        logger.LogInformation(
                            $"Removing old message for {user.UsernameWithDiscriminator}: {currentTime} - {message.Timestamp} = {(currentTime - message.Timestamp)}");
                        messages.TryDequeue(out message);
                    }

                    // Skip empty queues
                    if (messages.IsEmpty) { continue; }

                    IGrouping<string, DiscordMessage>? duplicateMessages;
                    int numberDuplicates;

                    // Now check if any of the remaining messages are identical
                    (duplicateMessages, numberDuplicates) = messages.GroupBy(m => m.Content)
                        .Select(g => (Messages: g, Count: g.Count()))
                        .MaxBy(t => t.Count);

                    if (numberDuplicates < MaxDuplicateMessages) { continue; }

                    logger.LogInformation(
                        "Deleting messages sent by and muting {User} for reason {Reason} (sent {Count} messages with content {Content})",
                        user.UsernameWithDiscriminator,
                        MessageDeleters.MessageDeletionReason.PotentialSpam,
                        numberDuplicates,
                        duplicateMessages.First().Content);

                    var channels = duplicateMessages.Select(m => m.Channel).DistinctBy(c => c.Id).ToArray();

                    string reason =
                        $"{numberDuplicates} copies of this message sent in the last {KeepDuration.TotalSeconds}s in {string.Join(", ", channels.Select(c => c.Mention))}.";
                    await BarotraumaToolBox.ReportSpam(botMain, duplicateMessages.First(), reason);
                    
                    await botMain.MuteUser(user, "Auto-detected spam messages");
                    await Task.WhenAll(duplicateMessages.Select(m => m.DeleteAsync("Auto-detected spam message")));
                    messages.Clear();
                }
                catch (Exception e)
                {
                    messages.TryPeek(out var sus);
                    logger.LogError($"Duplicate filter failed for user {sus?.Author?.UsernameWithDiscriminator ?? "[NULL]"}: {e}");
                    messages.Clear();
                }
            }
            await Task.Delay(LoopFrequency);
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
            if (!string.IsNullOrWhiteSpace(message?.Content)) { current.Enqueue(message); }
            return current;
        };

    public Task MessageCreated(DiscordClient sender, MessageCreateEventArgs args)
    {
        var timestamp = args.Message.Timestamp;
        var now = DateTimeOffset.Now;
        logger.LogInformation($"Timestamp: {timestamp}; Now: {now}");
        cache.AddOrUpdate(args.Message.Author, Add(args.Message), Update(args.Message));
        return Task.CompletedTask;
    }
}

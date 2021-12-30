using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.CommandsNext.Exceptions;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;

namespace WatcherBot.Utils
{
    public class DuplicateMessageFilter
    {
        private static readonly TimeSpan LoopFrequency = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan KeepDuration = TimeSpan.FromSeconds(5);
        private const int MaxDuplicateMessages = 3;
        private readonly ConcurrentDictionary<DiscordUser, ConcurrentQueue<DiscordMessage>> cache;

        public DuplicateMessageFilter()
        {
            cache = new ConcurrentDictionary<DiscordUser, ConcurrentQueue<DiscordMessage>>();
            Task.Run(MainLoop);
        }

        private async Task MainLoop()
        {
            while (true)
            {
                DateTimeOffset currentTime = DateTimeOffset.Now;
                foreach ((DiscordUser user, ConcurrentQueue<DiscordMessage> messages) in cache)
                {
                    // Skip empty queues
                    if (messages.IsEmpty) { continue; }

                    IGrouping<int, DiscordMessage>? duplicateMessages;
                    int numberDuplicates;
                    lock (messages)
                    {
                        // Remove old messages
                        while (messages.TryPeek(out DiscordMessage? message)
                               && currentTime - message.Timestamp >= KeepDuration)
                        {
                            messages.TryDequeue(out message);
                        }

                        // Now check if any of the remaining messages are identical
                        (duplicateMessages, numberDuplicates) = messages
                                                   .GroupBy(m => m.Content.GetHashCode())
                                                   .Select(g => (Messages: g, Count: g.Count()))
                                                   .MaxBy(t => t.Count);
                    }

                    if (numberDuplicates < MaxDuplicateMessages) { continue; }

                    await Task.WhenAll(duplicateMessages.Select(m => m.DeleteAsync("Auto-detected spam message")));
                    // mute user
                }
                await Task.Delay(LoopFrequency);
            }
        }

        private static Func<DiscordUser, ConcurrentQueue<DiscordMessage>> Add(DiscordMessage message) => _ =>
        {
            ConcurrentQueue<DiscordMessage> queue = new();
            queue.Enqueue(message);
            return queue;
        };

        private static Func<DiscordUser, ConcurrentQueue<DiscordMessage>, ConcurrentQueue<DiscordMessage>> Update(DiscordMessage message) => (_, current) =>
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
}

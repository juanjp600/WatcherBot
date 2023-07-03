using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WatcherBot.Utils;

public class ThreadKeepAlive : LoopingTask
{
    private readonly DiscordClient client;
    private readonly string keepAliveMessage;
    private readonly IReadOnlySet<ulong> keepAliveThreadIds;

    public ThreadKeepAlive(BotMain botMain, Config.Config config) : base(botMain, config)
    {
        client             = botMain.Client;
        keepAliveThreadIds = config.KeepAliveThreadIds;
        keepAliveMessage   = config.KeepAliveMessage;
    }

    protected override TimeSpan LoopFrequency => TimeSpan.FromSeconds(30);

    protected override Task LoopWork() => Task.WhenAll(keepAliveThreadIds.Select(CheckThread));

    private async Task CheckThread(ulong id)
    {
        Logger.LogInformation("Checking thread {ThreadId} for archival prevention", id);
        DiscordThreadChannel thread = await client.GetThreadAsync(id, true);

        if (thread.ThreadMetadata.Locked == true)
        {
            Logger.LogInformation("Thread {ThreadId} is locked", id);
            return;
        }

        if (thread.ThreadMetadata.Archived)
        {
            Logger.LogInformation("Unarchiving thread {ThreadId}", id);
            await thread.UnarchiveAsync();
            return;
        }

        if (!thread.LastMessageId.HasValue)
        {
            Logger.LogInformation("Cannot find last message for thread {ThreadId}", id);
            return;
        }

        DiscordMessage lastMessage = await thread.GetMessageAsync(thread.LastMessageId.Value);

        Logger.LogDebug("Last message contents is {LastMessage}", lastMessage.Content);

        bool willArchive = DateTimeOffset.Now + LoopFrequency - lastMessage.Timestamp
                           >= thread.ThreadMetadata.AutoArchiveDuration.ToTimeSpan();

        if (!willArchive)
        {
            Logger.LogInformation("Thread {ThreadId} will not be archived before next check", id);
            return;
        }

        Logger.LogInformation("Sending keep alive message for thread {ThreadId}", id);

        await thread.SendMessageAsync(keepAliveMessage);
    }
}

using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WatcherBot.Utils;

namespace WatcherBot.Commands;

public class PurgeCommandModule : BaseCommandModule
{
    [Command("purge")]
    [Description("Delete a given amount of messages, starting from a given messages" +
        " in a direction indicated by sign (negative => up, positive => down).")]
    [RequirePermissionInGuild(Permissions.ManageMessages)]
    [RequireModeratorRoleInGuild]
    public async Task Purge(CommandContext context, DiscordMessage msg, int count = 1024, [RemainingText] string? reason = null) {
        Func<ulong, int, Task<IReadOnlyList<DiscordMessage>>> getMessages
            = count < 0 ? msg.Channel.GetMessagesBeforeAsync : msg.Channel.GetMessagesAfterAsync;
        IEnumerable<DiscordMessage> messages = await getMessages(msg.Id, Math.Abs(count) - 1);
        messages = Enumerable.Append(messages, msg);

        var messageGroups = messages.GroupBy(m => m.Timestamp.Offset > TimeSpan.FromDays(14));
        foreach (var group in messageGroups)
        {
            if (group.Key)
            {
                // Manually clean old messages
                foreach (var m in group)
                {
                    m.DeleteAsync();
                }
            }
            else
            {
                msg.Channel.DeleteMessagesAsync(group, reason);
            }
        }
    }

    [Command("purge")]
    [Description("Delete a given amount of messages above the invocation.")]
    [RequirePermissionInGuild(Permissions.ManageMessages)]
    [RequireModeratorRoleInGuild]
    public async Task Purge(CommandContext context, ushort count, [RemainingText] string? reason = null)
        => Purge(context, context.Message, -count - 1, reason);
}

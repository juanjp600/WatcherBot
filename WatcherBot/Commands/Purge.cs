using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using WatcherBot.Utils;

namespace WatcherBot.Commands;

public class PurgeCommandModule : BaseCommandModule
{
    [Command("purge")]
    [Description("Delete a given amount of messages, starting from a given messages"
                 + " in a direction indicated by sign (negative => up, positive => down).")]
    [RequirePermissionInGuild(Permissions.ManageMessages)]
    [RequireModeratorRoleInGuild]
    public async Task Purge(CommandContext context, DiscordMessage msg, int count = 1024, [RemainingText] string? reason = null) {

        Func<ulong, int, Task<IReadOnlyList<DiscordMessage>>> getMessages =
            count < 0 ? msg.Channel.GetMessagesBeforeAsync : msg.Channel.GetMessagesAfterAsync;

        IEnumerable<DiscordMessage> messages = await getMessages(msg.Id, Math.Abs(count) - 1);
        messages = messages.Append(msg);

        IEnumerable<IGrouping<bool, DiscordMessage>> messageGroups =
            messages.GroupBy(m => m.Timestamp.Offset > TimeSpan.FromDays(14));

        foreach (IGrouping<bool, DiscordMessage> group in messageGroups)
        {
            if (group.Key)
            {
                // Manually clean old messages
                foreach (DiscordMessage m in group)
                {
#pragma warning disable CS4014
                    m.DeleteAsync(reason);
#pragma warning restore CS4014
                }
            }
            else
            {
#pragma warning disable CS4014
                msg.Channel.DeleteMessagesAsync(group, reason);
#pragma warning restore CS4014
            }
        }
    }

    [Command("purge")]
    [Description("Delete a given amount of messages above the invocation.")]
    [RequirePermissionInGuild(Permissions.ManageMessages)]
    [RequireModeratorRoleInGuild]
    public async Task Purge(CommandContext context, ushort count, [RemainingText] string? reason = null) =>
        await Purge(context, context.Message, -count - 1, reason);
}

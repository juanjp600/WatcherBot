using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using Octokit;

namespace WatcherBot.Utils;

public static class BarotraumaToolBox
{
    public static bool ContainsLink(this string content) =>
        content.CountSubstrings("https://") + content.CountSubstrings("http://") > 0;

    public static string StringConcat(this IEnumerable<string> enumerable, char separator) =>
        string.Join(separator, enumerable);

    public static string StringConcat(this IEnumerable<string> enumerable, string separator) =>
        string.Join(separator, enumerable);

    public static int CountLabelWeighting(this Issue issue, IReadOnlyDictionary<string, int> labelWeighting) =>
        issue.Labels.Sum(l => labelWeighting.GetValueOrDefault(l.Name));

    public static IEnumerable<(int, T)> Indexed<T>(this IEnumerable<T> source) => source.Select((x, i) => (i, x));

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (T t in source)
        {
            action(t);
        }
    }

    public static Task[] ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action) =>
        source.Select(action).ToArray();

    public static MemoryStream ToMemoryStream(this string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        return new MemoryStream(bytes);
    }

    private static async Task<DiscordDmChannel?> GetDmChannelAsync(this CommandContext context)
    {
        if (context.Channel is DiscordDmChannel dmChannel)
        {
            return dmChannel;
        }

        if (context.Member is not null)
        {
            return await context.Member.CreateDmChannelAsync();
        }

        return null;
    }

    public static async Task RespondDmAsync(this CommandContext context, Action<DiscordMessageBuilder> action)
    {
        DiscordDmChannel? dmChannel = await context.GetDmChannelAsync();
        if (dmChannel is not null)
        {
            await dmChannel.SendMessageAsync(action);
        }
    }

    public static async Task RespondDmAsync(this CommandContext context, DiscordEmbed embed)
    {
        DiscordDmChannel? dmChannel = await context.GetDmChannelAsync();
        if (dmChannel is not null)
        {
            await dmChannel.SendMessageAsync(embed);
        }
    }

    public static async Task RespondDmAsync(this CommandContext context, DiscordMessageBuilder builder)
    {
        DiscordDmChannel? dmChannel = await context.GetDmChannelAsync();
        if (dmChannel is not null)
        {
            await dmChannel.SendMessageAsync(builder);
        }
    }

    public static async Task RespondDmAsync(this CommandContext context, string content)
    {
        DiscordDmChannel? dmChannel = await context.GetDmChannelAsync();
        if (dmChannel is not null)
        {
            await dmChannel.SendMessageAsync(content);
        }
    }

    public static async Task RespondDmAsync(this CommandContext context, string content, DiscordEmbed embed)
    {
        DiscordDmChannel? dmChannel = await context.GetDmChannelAsync();
        if (dmChannel is not null)
        {
            await dmChannel.SendMessageAsync(content, embed);
        }
    }

    public static int CountSubstrings(this string str, string substr)
    {
        var count = 0;
        var index = 0;
        while (true)
        {
            index = str.IndexOf(substr, index, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                break;
            }

            index++;
            count++;
        }

        return count;
    }

    public static bool ToBool(this IsCringe cringe) => cringe == IsCringe.Yes;

    public static IsCringe ToCringe(this bool @bool) => @bool ? IsCringe.Yes : IsCringe.No;

    public static async Task ReportSpam(BotMain botMain, DiscordMessage spamMessage, string reason)
    {
        DiscordChannel    reportChannel = botMain.SpamReportChannel;
        DiscordMember     member        = await botMain.GetMemberFromUser(spamMessage.Author);
        DiscordDmChannel? dmChannel     = await member.CreateDmChannelAsync();
        DiscordMessage?   dm            = null;
        Exception?        dmException   = null;
        try
        {
            dm =
                await
                    dmChannel
                        .SendMessageAsync($"You have been automatically muted on the Undertow Games server for sending the following message in {spamMessage.Channel.Mention}:\n\n"
                                          + $"```\n{spamMessage.Content.Replace("`", "")}\n```\n\n"
                                          + "This is a spam prevention measure. If this was a false positive, please contact a moderator or administrator.");
        }
        catch (Exception e)
        {
            dmException = e;
        }

        if (!spamMessage.Channel.IsPrivate)
        {
            _ =
                reportChannel
                    .SendMessageAsync($"{spamMessage.Author.Mention} has been muted for sending the following message in {spamMessage.Channel.Mention}:\n\n"
                                      + $"```\n{spamMessage.Content.Replace("`", "")}\n```\n\n"
                                      + $"{reason}\n\n"
                                      + "If this was a false positive, you may revert this by removing the `Muted` role and granting the `Spam filter exemption` role.\n\n"
                                      + (dm != null
                                             ? "The user has been informed via DM."
                                             : "The user **could not** be informed via DM.\n\n")
                                      + (dmException != null
                                             ? $"The following exception was thrown: {dmException}"
                                             : ""));
        }
    }

    public static TimeSpan ToTimeSpan(this ThreadAutoArchiveDuration duration) => TimeSpan.FromMinutes((int)duration);
}

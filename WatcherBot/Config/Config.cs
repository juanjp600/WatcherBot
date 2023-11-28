using System;
using System.Collections.Generic;
using System.Linq;
using WatcherBot.Utils;
using Range = WatcherBot.Utils.Range;

namespace WatcherBot.Config;

public class Config
{
    public const string ConfigSection = "Watcher";
    private readonly Lazy<Dictionary<ulong, Range>> attachmentLimitsLazy;

    public Config()
    {
        attachmentLimitsLazy =
            new Lazy<Dictionary<ulong, Range>>(() => attachmentLimits.ToDictionary(kvp => Convert.ToUInt64(kvp.Key),
                                                   kvp => kvp.Value));
    }

    public string DiscordApiToken { get; init; } = "";
    public string GitHubToken { get; init; } = "";
    public ulong OutputGuildId { get; init; }

    private HashSet<ulong> moderatorRoleIds { get; } = new();
    public IReadOnlySet<ulong> ModeratorRoleIds => moderatorRoleIds;

    private string formattingCharacters { get; } = "";
    public IReadOnlySet<char> FormattingCharacters => formattingCharacters.ToHashSet();

    private HashSet<ulong> prohibitCommandsFromUsers { get; } = new();
    public IReadOnlySet<ulong> ProhibitCommandsFromUsers => prohibitCommandsFromUsers;

    private HashSet<ulong> invitesAllowedOnChannels { get; } = new();
    public IReadOnlySet<ulong> InvitesAllowedOnChannels => invitesAllowedOnChannels;

    private HashSet<ulong> invitesAllowedOnServers { get; } = new();
    public IReadOnlySet<ulong> InvitesAllowedOnServers => invitesAllowedOnServers;

    private HashSet<ulong> cringeChannels { get; } = new();
    public IReadOnlySet<ulong> CringeChannels => cringeChannels;

    // ReSharper disable once CollectionNeverUpdated.Local
    private Dictionary<string, Range> attachmentLimits { get; } = new();
    public IReadOnlyDictionary<ulong, Range> AttachmentLimits => attachmentLimitsLazy.Value;

    private HashSet<ulong> noReplies { get; } = new();
    public IReadOnlySet<ulong> NoReplies => noReplies;

    public Templates Templates { get; init; } = new();

    private Spam Spam { get; } = new();

    private BadWords BadWords { get; } = new();

    public IReadOnlySet<(string Substring, int MaxDistance, float Weight)> SpamSubstrings =>
        Spam.SpamSubstrings;

    public (string InText, string InFilter, float Weight)[] GetSpamFilterHits(string messageContentToTest)
    {
        char[] whitespace = new[] { '\n', '\t', '\r', ' ' }.Concat(messageContentToTest.Where(char.IsWhiteSpace))
            .Distinct()
            .ToArray();
        string[] messageContentSplit = messageContentToTest.Split(whitespace);
        (string InText, string InFilter, float Weight)[] hits = messageContentSplit
            .Where(s1 => !string.IsNullOrWhiteSpace(s1))
            .Select(s1 => SpamSubstrings
                    .FirstOrDefault(s2 =>
                        LevenshteinDistance
                            .Calculate(s1,
                                s2
                                    .Substring)
                        <= s2
                            .MaxDistance)
                is var (substring, _, weight)
                ? (InText: s1, InFilter: substring, Weight: weight)
                : (InText: "", InFilter: "", Weight: 0.0f))
            .Where(t => t.Weight > 0.0f)
            .ToArray();
        return hits;
    }

    public IReadOnlySet<(string Substring, int MaxDistance)> BadSubstrings =>
        BadWords.BadSubstrings;

    private HashSet<string> knownSafeSubstrings { get; } = new();
    public IReadOnlySet<string> KnownSafeSubstrings => knownSafeSubstrings;

    public ulong MutedRole { get; init; }

    private HashSet<ulong> prohibitFormattingFromUsers { get; } = new();
    public IReadOnlySet<ulong> ProhibitFormattingFromUsers => prohibitFormattingFromUsers;

    public Issues Issues { get; } = new();

    public ulong SpamFilterExemptionRole { get; init; }
    public ulong SpamReportChannel { get; init; }

    private HashSet<ulong> keepAliveThreadIds { get; } = new();
    public IReadOnlySet<ulong> KeepAliveThreadIds => keepAliveThreadIds;

    public string KeepAliveMessage { get; init; } = "";
}

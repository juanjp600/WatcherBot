using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Range = WatcherBot.Utils.Range;

namespace WatcherBot.Config;

public class Spam
{
    private readonly Lazy<IReadOnlySet<(string Substring, int MaxDistance, float Weight)>> spam;

    public Spam()
    {
        spam = new Lazy<IReadOnlySet<(string Substring, int MaxDistance, float Weight)>>(() =>
            spamSubstrings.Zip(spamSubstringMaxDist)
                          .Zip(spamSubstringWeights, (tup, fl) => (tup.First, tup.Second, fl))
                          .ToImmutableHashSet());
    }

    private string[] spamSubstrings { get; } = Array.Empty<string>();
    private float[] spamSubstringWeights { get; } = Array.Empty<float>();
    private int[] spamSubstringMaxDist { get; } = Array.Empty<int>();
    public IReadOnlySet<(string Substring, int MaxDistance, float Weight)> SpamSubstrings => spam.Value;
}

public class Config : IConfigureOptions<IConfigurationRoot>
{
    public const string ConfigSection = "Watcher";

    public IConfigurationRoot Configuration { get; private set; } = null!;

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

    private Dictionary<ulong, Range> attachmentLimits { get; } = new();
    public IReadOnlyDictionary<ulong, Range> AttachmentLimits => attachmentLimits;

    public BanTemplate BanTemplate { get; init; } = new();

    private Spam spamSubstrings { get; } = new();

    public IReadOnlySet<(string Substring, int MaxDistance, float Weight)> SpamSubstrings =>
        spamSubstrings.SpamSubstrings;

    private HashSet<string> knownSafeSubstrings { get; } = new();
    public IReadOnlySet<string> KnownSafeSubstrings => knownSafeSubstrings;

    public ulong MutedRole { get; init; }


    private HashSet<ulong> prohibitFormattingFromUsers { get; } = new();
    public IReadOnlySet<ulong> ProhibitFormattingFromUsers => prohibitFormattingFromUsers;
    public ulong SpamFilterExemptionRole { get; init; }
    public ulong SpamReportChannel { get; init; }

    public void Configure(IConfigurationRoot options)
    {
        Configuration = options;
    }
}

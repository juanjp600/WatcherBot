using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using WatcherBot.Utils;

namespace WatcherBot.Config;

public class Config : IConfigureOptions<IConfigurationRoot>
{
    public const string ConfigSection = "Watcher";

    private Dictionary<ulong, Range> attachmentLimits { get; } = new();
    public IReadOnlyDictionary<ulong, Range> AttachmentLimits => attachmentLimits;
    public BanTemplate BanTemplate { get; init; }
    public IConfigurationRoot Configuration { get; private set; } = null!;

    private HashSet<ulong> cringeChannels { get; } = new();
    public IReadOnlySet<ulong> CringeChannels => cringeChannels;
    public string DiscordApiToken { get; init; } = "";
    private HashSet<char> formattingCharacters { get; } = new();
    public IReadOnlySet<char> FormattingCharacters => formattingCharacters;

    public string GitHubToken { get; init; } = "";
    private HashSet<ulong> invitesAllowedOnChannels { get; } = new();
    public IReadOnlySet<ulong> InvitesAllowedOnChannels => invitesAllowedOnChannels;
    private HashSet<ulong> invitesAllowedOnServers { get; } = new();
    public IReadOnlySet<ulong> InvitesAllowedOnServers => invitesAllowedOnServers;

    private HashSet<string> knownSafeSubstrings { get; } = new();
    public IReadOnlySet<string> KnownSafeSubstrings => knownSafeSubstrings;
    private HashSet<ulong> moderatorRoleIds { get; } = new();
    public IReadOnlySet<ulong> ModeratorRoleIds => moderatorRoleIds;
    public ulong MutedRole { get; init; }
    public ulong OutputGuildId { get; init; }
    private HashSet<ulong> prohibitCommandsFromUsers { get; } = new();
    public IReadOnlySet<ulong> ProhibitCommandsFromUsers => prohibitCommandsFromUsers;

    private HashSet<ulong> prohibitFormattingFromUsers { get; } = new();
    public IReadOnlySet<ulong> ProhibitFormattingFromUsers => prohibitFormattingFromUsers;
    public ulong SpamFilterExemptionRole { get; init; }
    public ulong SpamReportChannel { get; init; }

    private HashSet<(string Substring, int MaxDistance, float Weight)> spamSubstrings { get; } = new();
    public IReadOnlySet<(string Substring, int MaxDistance, float Weight)> SpamSubstrings => spamSubstrings;

    public void Configure(IConfigurationRoot options)
    {
        Configuration = options;
    }
}

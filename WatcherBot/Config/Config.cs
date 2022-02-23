using System.Collections.Generic;
using System.Linq;
using WatcherBot.Utils;

namespace WatcherBot.Config;

public class Config
{
    public const string ConfigSection = "Watcher";

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

    private Spam Spam { get; } = new();

    public IReadOnlySet<(string Substring, int MaxDistance, float Weight)> SpamSubstrings =>
        Spam.SpamSubstrings;

    private HashSet<string> knownSafeSubstrings { get; } = new();
    public IReadOnlySet<string> KnownSafeSubstrings => knownSafeSubstrings;

    public ulong MutedRole { get; init; }


    private HashSet<ulong> prohibitFormattingFromUsers { get; } = new();
    public IReadOnlySet<ulong> ProhibitFormattingFromUsers => prohibitFormattingFromUsers;
    public ulong SpamFilterExemptionRole { get; init; }
    public ulong SpamReportChannel { get; init; }
}

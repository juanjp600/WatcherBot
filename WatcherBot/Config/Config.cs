using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Range = WatcherBot.Utils.Range;

namespace WatcherBot.Config;

public class Config
{
    public readonly ImmutableDictionary<ulong, Range> AttachmentLimits;
    public readonly BanTemplate BanTemplate;
    public readonly IConfigurationRoot Configuration;

    public readonly ImmutableHashSet<ulong> CringeChannels;
    public readonly string DiscordApiToken;
    public readonly ImmutableHashSet<char> FormattingCharacters;

    public readonly string GitHubToken;
    public readonly ImmutableHashSet<ulong> InvitesAllowedOnChannels;
    public readonly ImmutableHashSet<ulong> InvitesAllowedOnServers;

    public readonly ImmutableHashSet<string> KnownSafeSubstrings;
    public readonly ImmutableHashSet<ulong> ModeratorRoleIds;
    public readonly ulong MutedRole;
    public readonly ulong OutputGuildId;
    public readonly ImmutableHashSet<ulong> ProhibitCommandsFromUsers;

    public readonly ImmutableHashSet<ulong> ProhibitFormattingFromUsers;
    public readonly ulong SpamFilterExemptionRole;
    public readonly ulong SpamReportChannel;

    public readonly ImmutableHashSet<(string Substring, int MaxDistance, float Weight)> SpamSubstrings;

    public readonly string YeensayMaskPath;

    public Config(IConfigurationBuilder builder)
    {
        Configuration = builder.Build();

        GitHubToken     = Configuration.GetSection("GitHubToken").Get<string>();
        OutputGuildId   = Configuration.GetSection("Target").Get<ulong>();
        DiscordApiToken = Configuration.GetSection("Token").Get<string>();

        BanTemplate = BanTemplate.FromConfig(this);

        //Cruelty :)
        CringeChannels       = Configuration.GetSection("CringeChannels").Get<ulong[]>().ToImmutableHashSet();
        FormattingCharacters = Configuration.GetSection("FormattingCharacters").Get<string>().ToImmutableHashSet();
        InvitesAllowedOnChannels =
            Configuration.GetSection("InvitesAllowedOnChannels").Get<ulong[]>().ToImmutableHashSet();
        InvitesAllowedOnServers =
            Configuration.GetSection("InvitesAllowedOnServers").Get<ulong[]>().ToImmutableHashSet();
        ModeratorRoleIds = Configuration.GetSection("ModeratorRoles").Get<ulong[]>().ToImmutableHashSet();
        AttachmentLimits = Configuration.GetSection("AttachmentLimits")
                                        .GetChildren()
                                        .ToImmutableDictionary(c => ulong.Parse(c.Key),
                                                               c => new Range(c.Get<string>()));
        ProhibitCommandsFromUsers =
            (Configuration.GetSection("ProhibitCommandsFromUsers").Get<ulong[]>() ?? Enumerable.Empty<ulong>())
            .ToImmutableHashSet();
        ProhibitFormattingFromUsers =
            (Configuration.GetSection("ProhibitFormattingFromUsers").Get<ulong[]>() ?? Enumerable.Empty<ulong>())
            .ToImmutableHashSet();

        //Spam detection
        IEnumerable<string> spamSubstrs = Configuration.GetSection("SpamSubstrings").Get<string[]>()
                                          ?? Enumerable.Empty<string>();
        IEnumerable<int> spamSubstrMaxDist =
            Configuration.GetSection("SpamSubstringMaxDist").Get<int[]>() ?? Enumerable.Empty<int>();
        IEnumerable<float> spamSubstrWeights =
            Configuration.GetSection("SpamSubstringWeights").Get<float[]>() ?? Enumerable.Empty<float>();
        SpamSubstrings = spamSubstrs.Zip(spamSubstrMaxDist, (s,  d) => (s, d))
                                    .Zip(spamSubstrWeights, (sd, w) => (sd.s, sd.d, w))
                                    .ToImmutableHashSet();
        Console.WriteLine(string.Join(", ", SpamSubstrings));
        KnownSafeSubstrings = (Configuration.GetSection("KnownSafeSubstrings").Get<string[]>()
                               ?? Enumerable.Empty<string>()).ToImmutableHashSet();
        SpamReportChannel       = Configuration.GetSection("SpamReportChannel").Get<ulong>();
        SpamFilterExemptionRole = Configuration.GetSection("SpamFilterExemptionRole").Get<ulong>();
        MutedRole               = Configuration.GetSection("MutedRole").Get<ulong>();

        YeensayMaskPath = Configuration.GetSection("YeensayMaskPath").Get<string>();
    }

    public static Config DefaultConfig() =>
        new(new ConfigurationBuilder().AddJsonFile("appsettings.json", false, false));
}

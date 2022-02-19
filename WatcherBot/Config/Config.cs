using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Range = WatcherBot.Utils.Range;
using WatcherBot.Utils;
using Serilog;

namespace WatcherBot.Config;

public class Config
{
    private readonly IConfigurationRoot Configuration;

    public readonly ImmutableDictionary<ulong, Range> AttachmentLimits;
    public readonly Templates Templates;

    public readonly IImmutableDictionary<ulong, ulong> GuildToLogChannel;

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

        var templatesSection = Configuration.GetSection(nameof(Templates));
        string arrSecToStr(string key) => string.Join("\n", templatesSection.GetSection(key).Get<string[]>());
        Templates = new Templates(
            Ban: arrSecToStr(nameof(Templates.Ban)),
            Timeout: arrSecToStr(nameof(Templates.Timeout)),
            DefaultAppealRecipient: templatesSection.GetSection(nameof(Templates.DefaultAppealRecipient)).Get<string>());

        GuildToLogChannel = Configuration.GetSection("LogChannels").GetChildren().ToImmutableDictionary(c => ulong.Parse(c.Key), c => ulong.Parse(c.Value));

        //Cruelty :)
        IEnumerable<T> getOrEmpty<T>(string value)
            => Configuration.GetSection(value).Get<T[]>() ?? Enumerable.Empty<T>();

        CringeChannels       = getOrEmpty<ulong>("CringeChannels").ToImmutableHashSet();
        FormattingCharacters = Configuration.GetSection("FormattingCharacters").Get<string>().ToImmutableHashSet();
        InvitesAllowedOnChannels = getOrEmpty<ulong>("InvitesAllowedOnChannels").ToImmutableHashSet();
        InvitesAllowedOnServers = getOrEmpty<ulong>("InvitesAllowedOnServers").ToImmutableHashSet();
        ModeratorRoleIds = getOrEmpty<ulong>("ModeratorRoles").ToImmutableHashSet();
        AttachmentLimits = Configuration.GetSection("AttachmentLimits")
                                        .GetChildren()
                                        .ToImmutableDictionary(c => ulong.Parse(c.Key),
                                                               c => new Range(c.Get<string>()));
        ProhibitCommandsFromUsers = getOrEmpty<ulong>("ProhibitCommandsFromUsers").ToImmutableHashSet();
        ProhibitFormattingFromUsers = getOrEmpty<ulong>("ProhibitFormattingFromUsers").ToImmutableHashSet();

        //Spam detection
        IEnumerable<string> spamSubstrs = getOrEmpty<string>("SpamSubstrings");
        IEnumerable<int> spamSubstrMaxDist = getOrEmpty<int>("SpamSubstringMaxDist");
        IEnumerable<float> spamSubstrWeights = getOrEmpty<float>("SpamSubstringWeights");
        SpamSubstrings = spamSubstrs.Zip(spamSubstrMaxDist, (s,  d) => (s, d))
                                    .Zip(spamSubstrWeights, (sd, w) => (sd.s, sd.d, w))
                                    .ToImmutableHashSet();
        Console.WriteLine(string.Join(", ", SpamSubstrings));
        KnownSafeSubstrings = getOrEmpty<string>("KnownSafeSubstrings").ToImmutableHashSet();
        SpamReportChannel       = Configuration.GetSection("SpamReportChannel").Get<ulong>();
        SpamFilterExemptionRole = Configuration.GetSection("SpamFilterExemptionRole").Get<ulong>();
        MutedRole               = Configuration.GetSection("MutedRole").Get<ulong>();

        YeensayMaskPath = Configuration.GetSection("YeensayMaskPath").Get<string>();
    }

    public ILogger CreateLogger()
        => new LoggerConfiguration().ReadFrom.Configuration(Configuration).CreateLogger();

    public static Config DefaultConfig() =>
        new(new ConfigurationBuilder().AddJsonFile("appsettings.json", false, false));
}

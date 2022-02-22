using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Range = WatcherBot.Utils.Range;
using WatcherBot.Utils;
using WatcherBot.Commands;
using Serilog;

namespace WatcherBot.Config;

public class Config
{
    private readonly IConfigurationRoot configuration;

    public readonly string CommandPrefix;

    public readonly ImmutableDictionary<ulong, Range> AttachmentLimits;
    public readonly Templates Templates;

    public readonly RawRoleAssignmentGuild[] RawRoleSelfAssignments;
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
        configuration = builder.Build();

        GitHubToken     = configuration.GetSection("GitHubToken").Get<string>();
        OutputGuildId   = configuration.GetSection("Target").Get<ulong>();
        DiscordApiToken = configuration.GetSection("Token").Get<string>();

        CommandPrefix = configuration.GetSection(CommandPrefix).Value;

        var templatesSection = configuration.GetSection(nameof(Templates));
        string arrSecToStr(string key) => string.Join("\n", templatesSection.GetSection(key).Get<string[]>());
        Templates = new Templates(
            Ban: arrSecToStr(nameof(Templates.Ban)),
            Timeout: arrSecToStr(nameof(Templates.Timeout)),
            DefaultAppealRecipient: templatesSection.GetSection(nameof(Templates.DefaultAppealRecipient)).Get<string>());

        RawRoleSelfAssignments = configuration.GetSection("RoleSelfAssignment").GetChildren()
            .Select(c => new RawRoleAssignmentGuild(
                ulong.Parse(c.Key), c.GetChildren()
                                     .Select(g => (command: CommandPrefix + g.Key, roleId: ulong.Parse(g.Value)))
                                     .ToArray()))
            .ToArray();


        //Cruelty :)
        IEnumerable<T> getOrEmpty<T>(string value)
            => configuration.GetSection(value).Get<T[]>() ?? Enumerable.Empty<T>();

        CringeChannels       = getOrEmpty<ulong>("CringeChannels").ToImmutableHashSet();
        FormattingCharacters = configuration.GetSection("FormattingCharacters").Get<string>().ToImmutableHashSet();
        InvitesAllowedOnChannels = getOrEmpty<ulong>("InvitesAllowedOnChannels").ToImmutableHashSet();
        InvitesAllowedOnServers = getOrEmpty<ulong>("InvitesAllowedOnServers").ToImmutableHashSet();
        ModeratorRoleIds = getOrEmpty<ulong>("ModeratorRoles").ToImmutableHashSet();
        AttachmentLimits = configuration.GetSection("AttachmentLimits")
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
        SpamReportChannel       = configuration.GetSection("SpamReportChannel").Get<ulong>();
        SpamFilterExemptionRole = configuration.GetSection("SpamFilterExemptionRole").Get<ulong>();
        MutedRole               = configuration.GetSection("MutedRole").Get<ulong>();

        YeensayMaskPath = configuration.GetSection("YeensayMaskPath").Get<string>();
    }

    public ILogger CreateLogger()
        => new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();

    public static Config DefaultConfig() =>
        new(new ConfigurationBuilder().AddJsonFile("appsettings.json", false, false));
}

using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Configuration;
using WatcherBot.Utils;

namespace WatcherBot.Config
{
    public record Config
    {
        // @formatter: off
        public readonly BanTemplate BanTemplate;
        public readonly IConfigurationRoot Configuration;

        public readonly string GitHubToken;
        public readonly ulong OutputGuildId;
        public readonly string DiscordApiToken;

        public readonly ImmutableHashSet<ulong> CringeChannels;
        public readonly ImmutableHashSet<char> FormattingCharacters;
        public readonly ImmutableHashSet<ulong> InvitesAllowedOnChannels;
        public readonly ImmutableHashSet<ulong> InvitesAllowedOnServers;
        public readonly ImmutableHashSet<ulong> ModeratorRoleIds;
        public readonly ImmutableDictionary<ulong, Range> AttachmentLimits;
        public readonly ImmutableHashSet<ulong> ProhibitCommandsFromUsers;

        public readonly ImmutableHashSet<ulong> ProhibitFormattingFromUsers;
        // @formatter:on

        public readonly ImmutableHashSet<(string Substring, int MaxDistance)> SpamSubstrings;
        public readonly ImmutableHashSet<string> KnownSafeSubstrings;
        public readonly ulong SpamReportChannel;
        public readonly ulong SpamFilterExemptionRole;
        public readonly ulong MutedRole;

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
            var spamSubstrs = Configuration.GetSection("SpamSubstrings").Get<string[]>() ?? Enumerable.Empty<string>();
            var spamSubstrMaxDist = Configuration.GetSection("SpamSubstringMaxDist").Get<int[]>() ?? Enumerable.Empty<int>();
            SpamSubstrings = spamSubstrs.Zip(spamSubstrMaxDist, (s, d) => (s, d))
                .ToImmutableHashSet();
            KnownSafeSubstrings = (Configuration.GetSection("KnownSafeSubstrings").Get<string[]>() ?? Enumerable.Empty<string>())
                .ToImmutableHashSet();
            SpamReportChannel = Configuration.GetSection("SpamReportChannel").Get<ulong>();
            SpamFilterExemptionRole = Configuration.GetSection("SpamFilterExemptionRole").Get<ulong>();
            MutedRole = Configuration.GetSection("MutedRole").Get<ulong>();
        }

        public static Config DefaultConfig() =>
            new(new ConfigurationBuilder().AddJsonFile("appsettings.json", false, false));
    }
}

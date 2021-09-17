using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Configuration;

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

        public readonly struct Range
        {
            public readonly int Min;
            public readonly int Max;

            public Range(int min, int max)
            {
                Min = min;
                Max = max;
            }

            public Range(string str)
            {
                string[]? split = str.Split(',');
                Min = int.Parse(split[0]);
                Max = int.Parse(split[1]);
            }

            public bool Contains(int v) => Min <= v && Max >= v;

            public override string ToString() => $"[{Min}, {Max}]";
        }

        public readonly ImmutableHashSet<ulong> CringeChannels;
        public readonly ImmutableHashSet<char> FormattingCharacters;
        public readonly ImmutableHashSet<ulong> InvitesAllowedOnChannels;
        public readonly ImmutableHashSet<ulong> InvitesAllowedOnServers;
        public readonly ImmutableHashSet<ulong> ModeratorRoleIds;
        public readonly ImmutableDictionary<ulong, Range> AttachmentLimits;
        public readonly ImmutableHashSet<ulong> ProhibitCommandsFromUsers;

        public readonly ImmutableHashSet<ulong> ProhibitFormattingFromUsers;
        // @formatter:on

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
        }

        public static Config DefaultConfig() =>
            new(new ConfigurationBuilder().AddJsonFile("appsettings.json", false, false));
    }
}

using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;

namespace Bot600
{
    public record Config
    {
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
        public readonly ImmutableHashSet<ulong> NoConversationsAllowedOnChannels;
        public readonly ImmutableHashSet<ulong> ProhibitCommandsFromUsers;
        public readonly ImmutableHashSet<ulong> ProhibitFormattingFromUsers;

        public Config(IConfigurationBuilder builder)
        {
            Configuration = builder.Build();

            GitHubToken = Configuration.GetSection("GitHubToken").Get<string>();
            OutputGuildId = Configuration.GetSection("Target").Get<ulong>();
            DiscordApiToken = Configuration.GetSection("Token").Get<string>();

            BanTemplate = BanTemplate.FromConfig(this);

            //Cruelty :)
            CringeChannels = Configuration.GetSection("CringeChannels").Get<ulong[]>().ToImmutableHashSet();
            FormattingCharacters = Configuration.GetSection("FormattingCharacters").Get<string>().ToImmutableHashSet();
            InvitesAllowedOnChannels =
                Configuration.GetSection("InvitesAllowedOnChannels").Get<ulong[]>().ToImmutableHashSet();
            InvitesAllowedOnServers =
                Configuration.GetSection("InvitesAllowedOnServers").Get<ulong[]>().ToImmutableHashSet();
            ModeratorRoleIds = Configuration.GetSection("ModeratorRoles").Get<ulong[]>().ToImmutableHashSet();
            NoConversationsAllowedOnChannels = Configuration.GetSection("NoConversationsAllowedOnChannels")
                                                            .Get<ulong[]>().ToImmutableHashSet();
            ProhibitCommandsFromUsers = Configuration.GetSection("ProhibitCommandsFromUsers").Get<ulong[]>()
                                                     .ToImmutableHashSet();
            ProhibitFormattingFromUsers = Configuration.GetSection("ProhibitFormattingFromUsers").Get<ulong[]>()
                                                       .ToImmutableHashSet();
        }

        public static Config DefaultConfig()
        {
            return new(new ConfigurationBuilder()
                           .AddJsonFile("appsettings.json", false, false));
        }
    }
}

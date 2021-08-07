using System.Collections.Immutable;
using System.Linq;
using DisCatSharp;
using DisCatSharp.Entities;
using Microsoft.Extensions.Configuration;

namespace Bot600.Config
{
    public record DiscordConfig
    {
        public readonly ImmutableHashSet<DiscordRole> ModeratorRoles;
        public readonly DiscordGuild OutputGuild;

        public DiscordConfig(Config config, DiscordClient client)
        {
            DiscordGuild outputGuild = client.GetGuildAsync(config.OutputGuildId).Result;
            OutputGuild = outputGuild;
            ModeratorRoles =
                (from id in config.Configuration.GetSection("ModeratorRoles").Get<ulong[]>()
                    let role = outputGuild.Roles[id]
                    where role is not null
                    select role).ToImmutableHashSet();
        }
    }
}

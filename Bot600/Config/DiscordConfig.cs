using System.Collections.Immutable;
using System.Linq;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Bot600
{
    public record DiscordConfig
    {
        public readonly RestGuild OutputGuild;
        public readonly ImmutableHashSet<RestRole> ModeratorRoles;

        public DiscordConfig(Config config, BaseSocketClient client)
        {
            RestGuild? outputGuild = client.Rest.GetGuildAsync(config.OutputGuildId).Result;
            OutputGuild = outputGuild;
            ModeratorRoles =
                (from id in config.Configuration.GetSection("ModeratorRoles").Get<ulong[]>()
                 let role = outputGuild.Roles.FirstOrDefault(r => r.Id == id)
                 where role is not null
                 select (RestRole) role)
                .ToImmutableHashSet();
        }
    }
}

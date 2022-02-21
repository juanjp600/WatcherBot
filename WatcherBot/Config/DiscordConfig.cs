using System.Collections.Immutable;
using System.Linq;
using DisCatSharp;
using DisCatSharp.Entities;
using Microsoft.Extensions.Configuration;

namespace WatcherBot.Config;

public class DiscordConfig
{
    public readonly ImmutableHashSet<DiscordRole> ModeratorRoles;
    public readonly DiscordGuild OutputGuild;

    public DiscordConfig(Config config, DiscordClient client)
    {
        OutputGuild = client.GetGuildAsync(config.OutputGuildId).Result;
        ModeratorRoles = (from id in config.ModeratorRoleIds
                          let role = OutputGuild.Roles[id]
                          where role is not null
                          select role).ToImmutableHashSet();
    }
}

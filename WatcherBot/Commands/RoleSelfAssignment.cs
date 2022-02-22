using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

#nullable enable

namespace WatcherBot.Commands;

using RoleAssignment = ImmutableDictionary<DiscordGuild, ImmutableDictionary<string, DiscordRole>>;

public record struct RawRoleAssignmentGuild(ulong guildId, (string command, ulong roleId)[] roles) { };

public class RoleSelfAssignment
{
    private readonly RoleAssignment roleAssignments;

    public RoleSelfAssignment(DiscordClient client, RawRoleAssignmentGuild[] guilds)
    {
        roleAssignments = guilds.Select(g => (guild: client.GetGuildAsync(g.guildId).Result, g.roles))
                                .ToImmutableDictionary(g => g.guild,
                                                       g => g.roles.ToImmutableDictionary(r => r.command,
                                                                                          r => g.guild.GetRole(r.roleId)));
    }

    public async Task Message(DiscordClient client, MessageCreateEventArgs args)
    {
        if (!roleAssignments.TryGetValue(args.Guild, out var commands))
        {
            return;
        }

        if (!commands.TryGetValue(args.Message.Content, out var role)) {
            return;
        }

        // Why the fuck does the event not provide a member??
        DiscordMember member = await args.Guild.GetMemberAsync(args.Message.Author.Id);
        if (member.Roles.Contains(role))
        {
            member.RevokeRoleAsync(role, args.Message.Content);
        }
        else
        {
            member.GrantRoleAsync(role, args.Message.Content);
        }
        args.Message.DeleteAsync();

        args.Handled = true;
    }
}


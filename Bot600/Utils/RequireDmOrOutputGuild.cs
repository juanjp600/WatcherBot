using System;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;

namespace Bot600.Utils
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequireDmOrOutputGuild : RequireOutputGuild
    {
        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help) => ctx.Channel is DiscordDmChannel || await base.ExecuteCheckAsync(ctx, help);
    }
}

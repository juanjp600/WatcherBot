using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;

namespace Bot600.Utils
{
    public class RequirePermissionInGuild : CheckBaseAttribute
    {
        private readonly Permissions permissions;

        public RequirePermissionInGuild(Permissions permissions) => this.permissions = permissions;

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var botMain = (BotMain?)ctx.Services.GetService(typeof(BotMain));
            if (botMain?.DiscordConfig.OutputGuild is null)
            {
                return false;
            }

            DiscordMember? member = await botMain.DiscordConfig.OutputGuild.GetMemberAsync(ctx.User.Id);

            return member?.Permissions.HasFlag(permissions) ?? false;
        }
    }
}

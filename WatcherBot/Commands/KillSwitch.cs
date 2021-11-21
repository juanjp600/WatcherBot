using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Microsoft.Extensions.Logging;
using WatcherBot.Utils;

namespace WatcherBot.Commands
{
    // ReSharper disable once UnusedType.Global
    public class KillSwitchModule : BaseCommandModule
    {
        private readonly BotMain botMain;

        public KillSwitchModule(BotMain bm) => botMain = bm;

        [Command("killswitch")]
        [Aliases("kill")]
        [Description("Kills the bot.")]
        [RequirePermissionInGuild(Permissions.ManageGuild)]
        [RequireModeratorRoleInGuild]
        public async Task KillSwitch(CommandContext context)
        {
            DiscordMember member = context.Member;
            if (await botMain.IsUserModerator(member) == IsModerator.No)
            {
                await context.RespondAsync($"Error executing !killswitch: {member.Mention} is not a moderator");
                return;
            }

            context.Client.Logger.LogInformation("Calling {Command} to shut down (invoked by {Moderator})",
                                                 context.Command.QualifiedName, context.User.UsernameWithDiscriminator);
            botMain.Kill();
        }
    }
}

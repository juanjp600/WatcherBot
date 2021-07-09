using System.Threading.Tasks;
using Bot600.Utils;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Bot600.Commands
{
    public class KillSwitchModule : ModuleBase<SocketCommandContext>
    {
        private readonly BotMain botMain;

        public KillSwitchModule(BotMain bm)
        {
            botMain = bm;
        }

        [Command("killswitch", RunMode = RunMode.Async)]
        [Summary("Kills the bot.")]
        [RequireUserPermission(GuildPermission.ManageGuild, ErrorMessage = "Only moderators can issue this command")]
        public async Task KillSwitch()
        {
            SocketUser? user = Context.Message.Author;
            if (await botMain.IsUserModerator(user) == IsModerator.No)
            {
                await ReplyAsync($"Error executing !killswitch: {user.Mention} is not a moderator");
                return;
            }

            botMain.Kill();
        }
    }
}

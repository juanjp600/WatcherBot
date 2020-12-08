using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bot600.Commands
{
    public class KillSwitchModule : ModuleBase<SocketCommandContext>
    {
        private BotMain botMain;
        public KillSwitchModule(BotMain bm)
        {
            botMain = bm;
        }

        [Command("killswitch", RunMode = RunMode.Async)]
        [Summary("Kills the bot.")]
        [RequireUserPermission(GuildPermission.ManageGuild, ErrorMessage = "Only moderators can issue this command")]
        public async Task KillSwitch()
        {
            var user = Context.Message.Author;
            if (!await botMain.IsModerator(user))
            {
                ReplyAsync($"Error executing !killswitch: <@{user.Id}> is not a moderator");
                return;
            }

            botMain.Kill();
        }
    }
}

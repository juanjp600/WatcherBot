using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Bot600.Commands
{
    public class BanCommandModule : ModuleBase<SocketCommandContext>
    {
        private BotMain botMain;
        private readonly string banTemplate;
        private readonly string defaultAppeal;
        public BanCommandModule(BotMain bm)
        {
            botMain = bm;
            banTemplate = File.ReadAllText("config/ban.txt");
            defaultAppeal = File.ReadAllText("config/defaultAppeal.txt");
        }

        [Command("ban", RunMode = RunMode.Async)]
        [Summary("Bans a player and sends them an appeal message.")]
        [RequireUserPermission(GuildPermission.BanMembers, ErrorMessage = "Only moderators can issue this command")]
        public async Task Ban([Summary("User")] ulong userId, [Remainder][Summary("Ban reason")] string reason = null)
        {
            await Ban(await Context.Client.Rest.GetUserAsync(userId), reason);
        }

        [Command("ban", RunMode = RunMode.Async)]
        [Summary("Bans a player and sends them an appeal message.")]
        [RequireUserPermission(GuildPermission.BanMembers, ErrorMessage = "Only moderators can issue this command")]
        public async Task Ban([Summary("User")] IUser user, [Remainder][Summary("Ban reason")] string reason = null)
        {
            var banner = Context.Message.Author;
            if (!await botMain.IsModerator(banner))
            {
                ReplyAsync($"Error executing !ban: <@{banner.Id}> is not a moderator");
                return;
            }
            var baneeDM = await user.GetOrCreateDMChannelAsync();

            string bannerStr = $"{banner.Username}#{banner.Discriminator}";
            if (bannerStr != defaultAppeal)
            {
                bannerStr = $"`{bannerStr}` or `{defaultAppeal}`";
            }
            else
            {
                bannerStr = $"`{bannerStr}`";
            }

            string banMsg = banTemplate
                .Replace("[reason]", reason ?? "No reason provided")
                .Replace("[banner]", bannerStr);

            IMessage directMsg = null;
            string feedback = $"<@{user.Id}> has been banned. The appeal message could not be sent.\n";
            try
            {
                directMsg = await baneeDM.SendMessageAsync(banMsg);
                if (directMsg != null && directMsg.Id != 0)
                {
                    feedback = $"<@{user.Id}> has been banned. The message sent was the following:\n{banMsg}";
                }
            }
            catch
            {
                directMsg = null;
            }
            Context.Guild.AddBanAsync(user, reason: reason);
            Context.Message.Channel.SendMessageAsync(feedback);
        }

        [Command("ban_anon", RunMode = RunMode.Async)]
        [Summary("Bans a player and sends them an appeal message.")]
        [RequireUserPermission(GuildPermission.BanMembers, ErrorMessage = "Only moderators can issue this command")]
        public async Task BanAnon([Summary("User")] ulong userId, [Remainder][Summary("Ban reason")] string reason = null)
        {
            await BanAnon(await Context.Client.Rest.GetUserAsync(userId), reason);
        }

        [Command("ban_anon", RunMode = RunMode.Async)]
        [Summary("Bans a player and sends them an appeal message.")]
        [RequireUserPermission(GuildPermission.BanMembers, ErrorMessage = "Only moderators can issue this command")]
        public async Task BanAnon([Summary("User")] IUser user, [Remainder][Summary("Ban reason")] string reason = null)
        {
            var banner = Context.Message.Author;
            if (!await botMain.IsModerator(banner))
            {
                ReplyAsync($"Error executing !ban_anon: <@{banner.Id}> is not a moderator");
                return;
            }
            var baneeDM = await user.GetOrCreateDMChannelAsync();
            string banMsg = banTemplate
                .Replace("[reason]", reason ?? "No reason provided")
                .Replace("[banner]", $"`{defaultAppeal}`");
            IMessage directMsg = null;
            string feedback = $"<@{user.Id}> has been banned. The appeal message could not be sent.\n";
            try
            {
                directMsg = await baneeDM.SendMessageAsync(banMsg);
                if (directMsg != null && directMsg.Id != 0)
                {
                    feedback = $"<@{user.Id}> has been banned. The message sent was the following:\n{banMsg}";
                }
            }
            catch
            {
                directMsg = null;
            }
            Context.Guild.AddBanAsync(user, reason: reason);
            Context.Message.Channel.SendMessageAsync(feedback);
        }
    }
}

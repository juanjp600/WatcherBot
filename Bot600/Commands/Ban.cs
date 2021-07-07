using System.Threading.Tasks;
using Bot600.Utils;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Bot600.Commands
{
    public class BanCommandModule : ModuleBase<SocketCommandContext>
    {
        private readonly BanTemplate banTemplate;
        private readonly BotMain botMain;

        public BanCommandModule(BotMain bm)
        {
            botMain = bm;
            banTemplate = bm.Config.BanTemplate;
        }

        private async Task BanMember(IUser user, string? reason, Anonymous anon = Anonymous.No)
        {
            SocketUser? banner = Context.Message.Author;
            if (await botMain.IsUserModerator(banner) == IsModerator.No)
            {
                ReplyAsync($"Error executing !ban: {banner.Mention} is not a moderator");
                return;
            }

            IDMChannel? baneeDm = await user.GetOrCreateDMChannelAsync();

            string bannerStr;
            if (anon == Anonymous.Yes)
            {
                bannerStr = banTemplate.DefaultAppeal;
            }
            else
            {
                bannerStr = banner.ToString();
                bannerStr = bannerStr != banTemplate.DefaultAppeal
                                ? $"`{bannerStr}` or `{banTemplate.DefaultAppeal}`"
                                : $"`{bannerStr}`";
            }

            string banMsg = banTemplate.Template
                                       .Replace("[reason]", reason ?? "No reason provided")
                                       .Replace("[banner]", bannerStr);

            string feedback = $"{user.Mention} has been banned. The appeal message could not be sent.\n";
            try
            {
                IMessage? directMsg = await baneeDm.SendMessageAsync(banMsg);
                if (directMsg is not null && directMsg.Id != 0)
                {
                    feedback = $"{user.Mention} has been banned. The message sent was the following:\n{banMsg}";
                }
            }
            catch
            {
                // ignored
            }

            Context.Guild.AddBanAsync(user, reason: reason);
            Context.Message.Channel.SendMessageAsync(feedback);
        }

        [Command("ban", RunMode = RunMode.Async)]
        [Summary("Bans a player and sends them an appeal message.")]
        [RequireUserPermission(GuildPermission.BanMembers, ErrorMessage = "Only moderators can issue this command")]
        public async Task Ban([Summary("User")] ulong userId, [Remainder] [Summary("Ban reason")] string? reason = null)
        {
            await Ban(await Context.Client.Rest.GetUserAsync(userId), reason);
        }

        [Command("ban", RunMode = RunMode.Async)]
        [Summary("Bans a player and sends them an appeal message.")]
        [RequireUserPermission(GuildPermission.BanMembers, ErrorMessage = "Only moderators can issue this command")]
        public async Task Ban([Summary("User")] IUser user, [Remainder] [Summary("Ban reason")] string? reason = null)
        {
            await BanMember(user, reason);
        }


        [Command("ban_anon", RunMode = RunMode.Async)]
        [Summary("Bans a player and sends them an appeal message.")]
        [RequireUserPermission(GuildPermission.BanMembers, ErrorMessage = "Only moderators can issue this command")]
        public async Task BanAnon([Summary("User")] ulong userId,
                                  [Remainder] [Summary("Ban reason")] string? reason = null)
        {
            await BanAnon(await Context.Client.Rest.GetUserAsync(userId), reason);
        }

        [Command("ban_anon", RunMode = RunMode.Async)]
        [Summary("Bans a player and sends them an appeal message.")]
        [RequireUserPermission(GuildPermission.BanMembers, ErrorMessage = "Only moderators can issue this command")]
        public async Task BanAnon([Summary("User")] IUser user,
                                  [Remainder] [Summary("Ban reason")] string? reason = null)
        {
            BanMember(user, reason, Anonymous.Yes);
        }
    }
}

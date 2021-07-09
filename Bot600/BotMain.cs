using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bot600.Models;
using Bot600.Utils;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Octokit;
using User = Bot600.Models.User;

namespace Bot600
{
    public class BotMain : IDisposable
    {
        private readonly DiscordSocketClient client;
        private readonly CommandService commandService;
        private readonly IServiceProvider commandServiceProvider;

        internal readonly Config Config;

        public readonly GitHubClient GitHubClient;

        private readonly WatcherDatabaseContext watcherDatabaseContext;

        private bool kill;

        public BotMain()
        {
            Config = Config.DefaultConfig();

            //GitHub API
            GitHubClient = new GitHubClient(new ProductHeaderValue("Bot600"));
            var gitHubCredentials = new Credentials(Config.GitHubToken);
            GitHubClient.Credentials = gitHubCredentials;
            GitHubClient.SetRequestTimeout(TimeSpan.FromSeconds(5));

            //Discord API
            client = new DiscordSocketClient();

            client.Log += BarotraumaToolBox.InternalLog;
            client.MessageReceived += ReceiveMessage;
            client.Ready += OnReady;

            commandService = new CommandService();
            commandServiceProvider = new CommandServiceProvider(this);
            commandService.AddModulesAsync(Assembly.GetEntryAssembly(),
                                           commandServiceProvider);

            // Database
            watcherDatabaseContext = new WatcherDatabaseContext();
        }

        private DiscordConfig? DiscordConfig { get; set; }

        public void Dispose()
        {
            client.Dispose();
            ((IDisposable) commandService).Dispose();
            watcherDatabaseContext.Dispose();
        }

        public void Kill()
        {
            kill = true;
        }

        public async Task<IsModerator> IsUserModerator(IUser user)
        {
            RestGuild guild = DiscordConfig.OutputGuild;
            RestGuildUser guildUser = user is RestGuildUser rgu ? rgu : await guild.GetUserAsync(user.Id);
            return Config.ModeratorRoleIds.Intersect(guildUser.RoleIds).Any() ? IsModerator.Yes : IsModerator.No;
        }

        private async Task ReceiveMessage(SocketMessage msg)
        {
            void DeleteMsg()
            {
                async Task Delete(Task<IsModerator> t)
                {
                    if (t.Result == IsModerator.No)
                    {
                        msg.DeleteAsync();
                    }
                }

                IsUserModerator(msg.Author).ContinueWith(Delete);
            }

            try
            {
                if (msg.Author.IsBot || msg is not SocketUserMessage usrMsg)
                {
                    return;
                }

                bool ContainsDisallowedInvite()
                {
                    if (Config.InvitesAllowedOnChannels.Contains(msg.Channel.Id))
                    {
                        return false;
                    }

                    SocketGuildChannel channel;
                    if (msg.Channel is SocketGuildChannel sgc)
                    {
                        channel = sgc;
                    }
                    else
                    {
                        return false;
                    }

                    if (Config.InvitesAllowedOnServers.Contains(channel.Guild.Id))
                    {
                        return false;
                    }

                    string[] invites = {"discord.gg/", "discord.com/invite", "discordapp.com/invite"};

                    return invites.Any(i => msg.Content.Contains(i, StringComparison.OrdinalIgnoreCase));
                }

                if (ContainsDisallowedInvite())
                {
                    DeleteMsg();
                    return;
                }

                static bool MessageHasOneAttachment(SocketMessage message)
                {
                    bool insecureLink = message.Content.Contains("http://", StringComparison.OrdinalIgnoreCase);
                    bool authorIsBot = message.Author.IsBot;
                    int numberWellSizedAttachments = message.Attachments.Count(a => a.Width is null
                                                                                   || a.Height is null
                                                                                   || a.Width >= 16 &&
                                                                                   a.Height >= 16);
                    int numberLinks = message.Content.CountSubstrings("https://");

                    return
                        (authorIsBot || numberWellSizedAttachments + numberLinks == 1)
                        && numberWellSizedAttachments == message.Attachments.Count
                        && !insecureLink;
                }

                if (Config.NoConversationsAllowedOnChannels.Contains(msg.Channel.Id) && !MessageHasOneAttachment(msg))
                {
                    DeleteMsg();
                    return;
                }

                if (Config.ProhibitFormattingFromUsers.Contains(msg.Author.Id) &&
                    msg.Content.Any(c => Config.FormattingCharacters.Contains(c)))
                {
                    DeleteMsg();
                    return;
                }

                IsCringe UserIsCringe()
                {
                    using var db = new WatcherDatabaseContext();
                    User user = User.GetOrCreateUser(db, usrMsg.Author.Id);
                    IsCringe channelIsCringe =
                        Config.CringeChannels.Contains(usrMsg.Channel.Id) ? IsCringe.Yes : IsCringe.No;
                    user.NewMessage(channelIsCringe);
                    db.SaveChanges();
                    // it's cringe to bool to cringe
                    return (channelIsCringe.ToBool() && user.IsCringe.ToBool()).ToCringe();
                }

                if (UserIsCringe() == IsCringe.Yes)
                {
                    DeleteMsg();
                    return;
                }

                await ParseCommand(usrMsg);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}\n{e.StackTrace}");
                throw;
            }
        }

        private async Task ParseCommand(SocketUserMessage msg)
        {
            await Task.Yield();
            var argPos = 0;

            if (Config.ProhibitCommandsFromUsers.Contains(msg.Author.Id) ||
                !(msg.HasCharPrefix('!', ref argPos) ||
                  msg.HasMentionPrefix(client.CurrentUser, ref argPos)) ||
                msg.Author.IsBot)
            {
                return;
            }

            var context = new SocketCommandContext(client, msg);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            commandService.ExecuteAsync(context,
                                        argPos,
                                        commandServiceProvider);
        }

        private async Task OnReady()
        {
            await Task.Yield();
            DiscordConfig = new DiscordConfig(Config, client);
        }

        public async Task MainAsync()
        {
            string token = Config.DiscordApiToken;

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            while (!kill)
            {
                await Task.Delay(1000);
            }

            await client.LogoutAsync();
        }
    }

    internal class CommandServiceProvider : IServiceProvider
    {
        private readonly BotMain botMain;

        public CommandServiceProvider(BotMain bm)
        {
            botMain = bm;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(BotMain))
            {
                return botMain;
            }

            throw new NotImplementedException();
        }
    }
}

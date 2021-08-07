using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bot600.Config;
using Bot600.Models;
using Bot600.Utils;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Octokit;

namespace Bot600
{
    public class BotMain : IDisposable
    {
        public readonly DiscordClient Client;

        internal readonly Config.Config Config;

        private readonly Lazy<DiscordConfig> discordConfig;

        public readonly GitHubClient GitHubClient;
        private readonly CancellationTokenSource shutdownRequest;

        private readonly WatcherDatabaseContext watcherDatabaseContext;

        public BotMain()
        {
            Config          = Bot600.Config.Config.DefaultConfig();
            shutdownRequest = new CancellationTokenSource();
            MessageDeleters deleters = new(this);

            //GitHub API
            GitHubClient = new GitHubClient(new ProductHeaderValue("Bot600"));
            var gitHubCredentials = new Credentials(Config.GitHubToken);
            GitHubClient.Credentials = gitHubCredentials;
            GitHubClient.SetRequestTimeout(TimeSpan.FromSeconds(5));

            //Discord API
            var config = new DiscordConfiguration
            {
                Token         = Config.DiscordApiToken,
                TokenType     = TokenType.Bot,
                Intents       = DiscordIntents.AllUnprivileged,
                AutoReconnect = true,
            };
            Client = new DiscordClient(config);

            discordConfig         =  new Lazy<DiscordConfig>(() => new DiscordConfig(Config, Client));
            Client.MessageCreated += HandleCommand;
            Client.MessageCreated += deleters.ContainsDisallowedInvite;
            Client.MessageCreated += deleters.DeleteCringeMessages;
            Client.MessageCreated += deleters.MessageHasOneAttachment;
            Client.MessageCreated += deleters.ProhibitFormattingFromUsers;

            ServiceProvider services = new ServiceCollection().AddSingleton(this).BuildServiceProvider();

            CommandsNextConfiguration commandsConfig = new()
            {
                DmHelp                   = true,
                EnableMentionPrefix      = true,
                Services                 = services,
                StringPrefixes           = new[] { "!" },
                UseDefaultCommandHandler = false,
            };
            CommandsNextExtension commands = Client.UseCommandsNext(commandsConfig);
            commands.RegisterCommands(Assembly.GetAssembly(typeof(BotMain)));

            // Database
            watcherDatabaseContext = new WatcherDatabaseContext();
        }

        public DiscordConfig DiscordConfig => discordConfig.Value;

        public void Dispose()
        {
            Client.Dispose();
            watcherDatabaseContext.Dispose();
        }

        public void Kill() => shutdownRequest.Cancel();

        public async Task<IsModerator> IsUserModerator(DiscordUser user)
        {
            DiscordGuild guild = DiscordConfig.OutputGuild;
            DiscordMember guildUser = user is DiscordMember rgu ? rgu : await guild.GetMemberAsync(user.Id);
            return DiscordConfig.ModeratorRoles.Intersect(guildUser.Roles).Any() ? IsModerator.Yes : IsModerator.No;
        }

        private Task HandleCommand(DiscordClient sender, MessageCreateEventArgs args)
        {
            if (!Config.ProhibitCommandsFromUsers.Contains(args.Author.Id))
            {
                return (typeof(CommandsNextExtension).GetMethod("HandleCommandsAsync",
                                                                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
                    sender.GetCommandsNext(),
                    new object?[] { sender, args }) as Task)!;
            }

            return Task.CompletedTask;
        }

        public async Task MainAsync()
        {
            await Client.ConnectAsync();
            while (!shutdownRequest.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }

            await Client.UpdateStatusAsync(null, UserStatus.Offline);
            await Client.DisconnectAsync();
            await Task.Delay(2500);
            Dispose();
        }
    }
}

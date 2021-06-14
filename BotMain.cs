using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Octokit;
using System.Collections.Immutable;

namespace Bot600
{
    public class BotMain
    {
        private bool kill = false;
        public void Kill()
        {
            kill = true;
        }

        internal IConfigurationRoot Config;

        private ulong outputGuildId;

        private RestGuild outputGuild;

        public GitHubClient GitHubClient { get; private set; }

        private HashSet<RestRole> moderatorRoles = new HashSet<RestRole>();
        public async Task<bool> IsModerator(IUser user)
        {
            var guild = await client.Rest.GetGuildAsync(outputGuildId);
            var guildUser = await guild.GetUserAsync(user.Id);
            return guildUser.RoleIds.Any(r1 => moderatorRoles.Any(r2 => r2.Id == r1));
        }

        public async Task InternalLog(LogMessage msg)
        {
            await Task.Yield();

            Console.WriteLine($"[{msg.Severity}] {msg.Source}: {msg.Message}");
            if (msg.Exception != null)
            {
                Console.WriteLine($"Exception: {msg.Exception.Message} {msg.Exception.StackTrace}");
            }
        }

        private DiscordSocketClient client;
        private CommandService commandService;
        private IServiceProvider commandServiceProvider;

        private async Task ReceiveMessage(SocketMessage msg)
        {
            try
            {
                if (!(msg is SocketUserMessage usrMsg)) { return; }
                await ParseCommand(usrMsg);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}\n{e.StackTrace}");
                throw;
            }
        }

        private ImmutableHashSet<char> formattingCharacters;
        private ImmutableHashSet<ulong> prohibitFormattingFromUsers;

        private ImmutableHashSet<ulong> noConversationsAllowedOnChannels;
        private ImmutableHashSet<ulong> prohibitCommandsFromUsers;
        private ImmutableHashSet<ulong> invitesAllowedOnChannels;
        private ImmutableHashSet<ulong> invitesAllowedOnServers;

        private static int CountSubstrs(string str, string substr)
        {
            int count = 0;
            int index = 0;
            while (true)
            {
                index = str.IndexOf(substr, index, StringComparison.OrdinalIgnoreCase);
                if (index < 0) { break; }
                index++;
                count++;
            }
            return count;
        }

        private async Task ParseCommand(SocketUserMessage msg)
        {
            if (!invitesAllowedOnChannels.Contains(msg.Channel.Id)
                && (msg.Channel is SocketGuildChannel sgc)
                && !invitesAllowedOnServers.Contains(sgc.Guild.Id)
                && (msg.Content.Contains("discord.gg/", StringComparison.OrdinalIgnoreCase) ||
                    msg.Content.Contains("discord.com/invite", StringComparison.OrdinalIgnoreCase) ||
                    msg.Content.Contains("discordapp.com/invite", StringComparison.OrdinalIgnoreCase)))
            {
                IsModerator(msg.Author).ContinueWith(async (t) =>
                {
                    if (!t.Result) { msg.DeleteAsync(); }
                });
                return;
            }

            if (noConversationsAllowedOnChannels.Contains(msg.Channel.Id))
            {
                if ((((msg.Attachments.Count(a => a.Width == null || a.Height == null || (a.Width >= 16 && a.Height >= 16)) + CountSubstrs(msg.Content, "https://")) != 1)
                    || msg.Attachments.Count(a => a.Width != null && a.Height != null && (a.Width < 16 || a.Height < 16)) > 0
                    || msg.Content.Contains("http://", StringComparison.OrdinalIgnoreCase))
                    && !msg.Author.IsBot)
                {
                    IsModerator(msg.Author).ContinueWith(async (t) =>
                    {
                        if (!t.Result) { msg.DeleteAsync(); }
                    });
                    return;
                }
            }

            if (prohibitFormattingFromUsers.Contains(msg.Author.Id) &&
                msg.Content.Any(c => formattingCharacters.Contains(c)))
            {
                msg.DeleteAsync();
                return;
            }

            int argPos = 0;

            if (prohibitCommandsFromUsers.Contains(msg.Author.Id) ||
                !(msg.HasCharPrefix('!', ref argPos) ||
                msg.HasMentionPrefix(client.CurrentUser, ref argPos)) ||
                msg.Author.IsBot)
            {
                return;
            }

            var context = new SocketCommandContext(client, msg);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            commandService.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: commandServiceProvider);
        }

        private async Task OnReady()
        {
            outputGuild = await client.Rest.GetGuildAsync(outputGuildId);
            moderatorRoles = Config.GetSection("ModeratorRoles").Get<ulong[]>()
                .Select(i => outputGuild.Roles.FirstOrDefault(r => r.Id == i))
                .ToHashSet();
        }

        public async Task MainAsync()
        {
            Config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true).Build();

            //GitHub API
            GitHubClient = new GitHubClient(new ProductHeaderValue("Bot600"));
            var gitHubCredentials = new Credentials(Config.GetSection("GitHubToken").Get<string>());
            GitHubClient.Credentials = gitHubCredentials;
            GitHubClient.SetRequestTimeout(TimeSpan.FromSeconds(5));

            //Discord API
            DiscordSocketConfig discordSocketConfig = new DiscordSocketConfig {MessageCacheSize = 0};
            client = new DiscordSocketClient();

            client.Log += InternalLog;
            client.MessageReceived += ReceiveMessage;
            client.Ready += OnReady;

            commandService = new CommandService();
            commandServiceProvider = new CommandServiceProvider(this);
            await commandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                                 services: commandServiceProvider);

            outputGuildId = Config.GetSection("Target").Get<ulong>();
            var token = Config.GetSection("Token").Get<string>();

            //Cruelty :)
            formattingCharacters = Config.GetSection("FormattingCharacters").Get<string>().ToImmutableHashSet();
            prohibitFormattingFromUsers = Config.GetSection("ProhibitFormattingFromUsers").Get<ulong[]>().ToImmutableHashSet();
            noConversationsAllowedOnChannels = Config.GetSection("NoConversationsAllowedOnChannels").Get<ulong[]>().ToImmutableHashSet();
            prohibitCommandsFromUsers = Config.GetSection("ProhibitCommandsFromUsers").Get<ulong[]>().ToImmutableHashSet();
            invitesAllowedOnChannels = Config.GetSection("InvitesAllowedOnChannels").Get<ulong[]>().ToImmutableHashSet();
            invitesAllowedOnServers = Config.GetSection("InvitesAllowedOnServers").Get<ulong[]>().ToImmutableHashSet();

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            while (!kill) { await Task.Delay(1000); }

            await client.LogoutAsync();
        }
    }

    class CommandServiceProvider : IServiceProvider
    {
        private BotMain botMain;
        public CommandServiceProvider(BotMain bm) : base()
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

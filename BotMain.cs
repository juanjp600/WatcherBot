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

namespace Bot600
{
    public class BotMain
    {
        private bool kill = false;
        public void Kill()
        {
            kill = true;
        }

        private ulong outputGuildId;

        private RestGuild outputGuild;

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

        private async Task ParseCommand(SocketUserMessage msg)
        {
            int argPos = 0;
            if (!(msg.HasCharPrefix('!', ref argPos) ||
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
            moderatorRoles.Clear();
            foreach (string idStr in File.ReadAllLines("config/moderatorroles.txt"))
            {
                ulong id = ulong.Parse(idStr);
                moderatorRoles.Add(outputGuild.Roles.First(r => r.Id == id));
            }
        }

        public async Task MainAsync()
        {
            if (!Directory.Exists("msgs"))
            {
                Directory.CreateDirectory("msgs");
            }

            DiscordSocketConfig config = new DiscordSocketConfig();
            config.MessageCacheSize = 0;
            client = new DiscordSocketClient();

            client.Log += InternalLog;
            client.MessageReceived += ReceiveMessage;
            client.Ready += OnReady;

            commandService = new CommandService();
            commandServiceProvider = new CommandServiceProvider(this);
            await commandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                                 services: commandServiceProvider);

            outputGuildId = ulong.Parse(File.ReadAllText("config/targets.txt"));
            var token = File.ReadAllText("config/token.txt");

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

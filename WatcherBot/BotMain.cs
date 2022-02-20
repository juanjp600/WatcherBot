using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using Serilog;
using WatcherBot.Config;
using WatcherBot.Logging;
using WatcherBot.Models;
using WatcherBot.Utils;

namespace WatcherBot;

public class BotMain : IDisposable
{
    public readonly DiscordClient Client;

    internal readonly Config.Config Config;

    private readonly Lazy<DiscordConfig> discordConfig;

    private readonly DuplicateMessageFilter duplicateMessageFilter;

    public readonly GitHubClient GitHubClient;
    private readonly CancellationTokenSource shutdownRequest;

    private readonly WatcherDatabaseContext watcherDatabaseContext;

    public BotMain()
    {
        Config     = WatcherBot.Config.Config.DefaultConfig();

        Log.Logger = Config.CreateLogger();
        Log.Logger.Information("Starting bot");
        shutdownRequest = new CancellationTokenSource();

        //GitHub API
        GitHubClient = new GitHubClient(new ProductHeaderValue("WatcherBot"));
        var gitHubCredentials = new Credentials(Config.GitHubToken);
        GitHubClient.Credentials = gitHubCredentials;
        GitHubClient.SetRequestTimeout(TimeSpan.FromSeconds(5));

        //Discord API
        var config = new DiscordConfiguration
        {
            Token         = Config.DiscordApiToken,
            TokenType     = TokenType.Bot,
            Intents       = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers,
            AutoReconnect = true,
            LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger),
        };
        Client = new DiscordClient(config);

        discordConfig         =  new Lazy<DiscordConfig>(() => new DiscordConfig(Config, Client));

        ServiceProvider services = new ServiceCollection().AddSingleton(this).BuildServiceProvider();

        EventRegistrar registrar = new(Client, services);
        registrar.RegisterEventHandlers(Assembly.GetAssembly(typeof(BotMain)) ?? throw new Exception("Failed to get assembly"));
        registrar.RegisterEventHandler(this);

        duplicateMessageFilter = new DuplicateMessageFilter(this);
        registrar.RegisterEventHandler(duplicateMessageFilter);
        duplicateMessageFilter.Start();

        CommandsNextConfiguration commandsConfig = new()
        {
            DmHelp                   = true,
            EnableMentionPrefix      = true,
            ServiceProvider          = services,
            StringPrefixes           = new[] { "!" },
            UseDefaultCommandHandler = false,
        };
        CommandsNextExtension commands = Client.UseCommandsNext(commandsConfig);
        commands.CommandExecuted += Logging.Logging.CommandExecuted;
        commands.CommandErrored  += Logging.Logging.CommandErrored;
        commands.RegisterConverter(new DateTimeConverter());
        commands.RegisterCommands(Assembly.GetAssembly(typeof(BotMain)));

        // Database
        watcherDatabaseContext = new WatcherDatabaseContext();
    }

    public DiscordConfig DiscordConfig => discordConfig.Value;

    public DiscordChannel SpamReportChannel => DiscordConfig.OutputGuild.Channels[Config.SpamReportChannel];

    public DiscordRole MutedRole => DiscordConfig.OutputGuild.Roles[Config.MutedRole];

    public void Dispose()
    {
        Client.Dispose();
        duplicateMessageFilter.Cancel();
        duplicateMessageFilter.Dispose();
        watcherDatabaseContext.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Kill() => shutdownRequest.Cancel();

    public async Task<IsModerator> IsUserModerator(DiscordUser user)
    {
        DiscordGuild  guild     = DiscordConfig.OutputGuild;
        DiscordMember guildUser = user is DiscordMember rgu ? rgu : await guild.GetMemberAsync(user.Id);
        return DiscordConfig.ModeratorRoles.Intersect(guildUser.Roles).Any() ? IsModerator.Yes : IsModerator.No;
    }

    public async Task<DiscordMember> GetMemberFromUser(DiscordUser user)
    {
        DiscordGuild guild = DiscordConfig.OutputGuild;
        return user is DiscordMember rgu ? rgu : await guild.GetMemberAsync(user.Id);
    }

    public async Task<IsExemptFromSpamFilter> IsUserExemptFromSpamFilter(DiscordUser user)
    {
        DiscordGuild  guild     = DiscordConfig.OutputGuild;
        DiscordMember guildUser = user is DiscordMember rgu ? rgu : await guild.GetMemberAsync(user.Id);
        return guildUser.Roles.Any(r => r.Id == Config.SpamFilterExemptionRole)
                   ? IsExemptFromSpamFilter.Yes
                   : IsExemptFromSpamFilter.No;
    }

    public async Task MuteUser(DiscordUser user, string reason)
    {
        DiscordGuild  guild     = DiscordConfig.OutputGuild;
        DiscordMember guildUser = user is DiscordMember rgu ? rgu : await guild.GetMemberAsync(user.Id);
        await guildUser.ReplaceRolesAsync(guildUser.Roles.Concat(new[] { MutedRole }), reason);
    }

    [DiscordEvent("MessageCreated")]
    private Task HandleCommand(DiscordClient sender, MessageCreateEventArgs args)
    {
        if (!Config.ProhibitCommandsFromUsers.Contains(args.Author.Id))
        {
            return (typeof(CommandsNextExtension).GetMethod("HandleCommandsAsync",
                                                            BindingFlags.Instance | BindingFlags.NonPublic)!
                                                 .Invoke(sender.GetCommandsNext(), new object?[] { sender, args }) as
                        Task) !;
        }

        return Task.CompletedTask;
    }

    public async Task MainAsync()
    {
        await Client.ConnectAsync();
        while (!shutdownRequest.IsCancellationRequested) { await Task.Delay(1000); }

        await Client.UpdateStatusAsync(null, UserStatus.Offline);
        await Client.DisconnectAsync();
        await Task.Delay(2500);
        Dispose();
    }
}

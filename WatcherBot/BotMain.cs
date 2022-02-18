using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using Serilog;
using WatcherBot.Config;
using WatcherBot.Models;
using WatcherBot.Utils;

namespace WatcherBot;

public class BotMain : IDisposable
{
    public readonly DiscordClient Client;

    private readonly Config.Config config;

    private readonly Lazy<DiscordConfig> discordConfig;

    private readonly DuplicateMessageFilter duplicateMessageFilter;

    public readonly GitHubClient GitHubClient;
    private readonly CancellationTokenSource shutdownRequest;

    private readonly WatcherDatabaseContext watcherDatabaseContext;

    public BotMain()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder()
                                               .AddJsonFile("appsettings.json", false, false)
                                               .Build();
        ServiceProvider services = new ServiceCollection()
                                   .AddSingleton(this)
                                   .AddOptions()
                                   .Configure<Config.Config>(configurationRoot.GetSection(Config.Config.ConfigSection),
                                                             binder => binder.BindNonPublicProperties = true)
                                   .BuildServiceProvider();

        config = services.GetRequiredService<IOptions<Config.Config>>().Value;
        {
            Console.WriteLine(config.DiscordApiToken);
            Console.WriteLine(config.GitHubToken);
            Console.WriteLine(config.OutputGuildId);
            Console.WriteLine(string.Join(", ", config.ModeratorRoleIds));
            Console.WriteLine(string.Join("", config.FormattingCharacters));
            Console.WriteLine(string.Join(", ", config.ProhibitCommandsFromUsers));
            Console.WriteLine(string.Join(", ", config.InvitesAllowedOnChannels));
            Console.WriteLine(string.Join(", ", config.InvitesAllowedOnServers));
            Console.WriteLine(string.Join(", ", config.CringeChannels));
            Console.WriteLine(string.Join(", ", config.AttachmentLimits));
            Console.WriteLine($"{config.BanTemplate.Template}\n{config.BanTemplate.DefaultAppeal}");
            Console.WriteLine(string.Join("; ", config.SpamSubstrings));
            Console.WriteLine(string.Join(", ", config.KnownSafeSubstrings));
            Console.WriteLine(config.MutedRole);
            Console.WriteLine(string.Join(", ", config.ProhibitFormattingFromUsers));
            Console.WriteLine(config.SpamFilterExemptionRole);
            Console.WriteLine(config.SpamReportChannel);
        }
        Environment.Exit(0);
        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configurationRoot).CreateLogger();

        Log.Logger.Information("Starting bot");
        shutdownRequest = new CancellationTokenSource();

        //GitHub API
        GitHubClient = new GitHubClient(new ProductHeaderValue("WatcherBot"));
        var gitHubCredentials = new Credentials(config.GitHubToken);
        GitHubClient.Credentials = gitHubCredentials;
        GitHubClient.SetRequestTimeout(TimeSpan.FromSeconds(5));

        //Discord API
        var discordConfiguration = new DiscordConfiguration
        {
            Token         = config.DiscordApiToken,
            TokenType     = TokenType.Bot,
            Intents       = DiscordIntents.AllUnprivileged,
            AutoReconnect = true,
            LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger),
        };
        Client = new DiscordClient(discordConfiguration);

        discordConfig         =  new Lazy<DiscordConfig>(() => new DiscordConfig(config, Client));
        Client.MessageCreated += HandleCommand;
        MessageDeleters deleters = new(this, config);
        Client.MessageCreated += deleters.ContainsDisallowedInvite;
        Client.MessageCreated += deleters.DeleteCringeMessages;
        Client.MessageCreated += deleters.MessageWithinAttachmentLimits;
        Client.MessageCreated += deleters.ProhibitFormattingFromUsers;
        Client.MessageCreated += deleters.DeletePotentialSpam;

        duplicateMessageFilter =  new DuplicateMessageFilter(this, config);
        Client.MessageCreated  += duplicateMessageFilter.MessageCreated;
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
        commands.RegisterCommands(Assembly.GetAssembly(typeof(BotMain)));

        // Database
        watcherDatabaseContext = new WatcherDatabaseContext();
    }

    public DiscordConfig DiscordConfig => discordConfig.Value;

    public DiscordChannel SpamReportChannel => DiscordConfig.OutputGuild.Channels[config.SpamReportChannel];

    public DiscordRole MutedRole => DiscordConfig.OutputGuild.Roles[config.MutedRole];

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
        return guildUser.Roles.Any(r => r.Id == config.SpamFilterExemptionRole)
                   ? IsExemptFromSpamFilter.Yes
                   : IsExemptFromSpamFilter.No;
    }

    public async Task MuteUser(DiscordUser user, string reason)
    {
        DiscordGuild  guild     = DiscordConfig.OutputGuild;
        DiscordMember guildUser = user is DiscordMember rgu ? rgu : await guild.GetMemberAsync(user.Id);
        await guildUser.ReplaceRolesAsync(guildUser.Roles.Concat(new[] { MutedRole }), reason);
    }

    private Task HandleCommand(DiscordClient sender, MessageCreateEventArgs args)
    {
        if (!config.ProhibitCommandsFromUsers.Contains(args.Author.Id))
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

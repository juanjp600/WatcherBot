using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using Serilog;
using WatcherBot.Utils;

namespace WatcherBot;

public class BotMain : IDisposable
{
    public readonly DiscordClient Client;

    private readonly Config.Config config;

    private readonly DuplicateMessageFilter duplicateMessageFilter;

    public readonly GitHubClient GitHubClient;

    private readonly ServiceProvider services;
    private readonly CancellationTokenSource shutdownRequest;
    private readonly ThreadKeepAlive threadKeepAlive;

    public BotMain()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder()
                                               .AddJsonFile("appsettings.json", false, false)
                                               .Build();
        services = new ServiceCollection()
                   .AddSingleton(this)
                   .AddSingleton(configurationRoot)
                   .AddOptions()
                   .Configure<Config.Config>(configurationRoot.GetSection(Config.Config.ConfigSection),
                                             binder => binder.BindNonPublicProperties = true)
                   .BuildServiceProvider();

        var configOptions = services.GetRequiredService<IOptions<Config.Config>>();
        config     = configOptions.Value;
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
            Intents       = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContent,
            AutoReconnect = true,
            LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger),
        };
        Client = new DiscordClient(discordConfiguration);

        Client.MessageCreated += HandleCommand;
        MessageDeleters deleters = new(this, configOptions);
        Client.MessageCreated += deleters.ContainsDisallowedInvite;
        Client.MessageCreated += deleters.DeleteCringeMessages;
        Client.MessageCreated += deleters.MessageWithinAttachmentLimits;
        Client.MessageCreated += deleters.ProhibitFormattingFromUsers;
        Client.MessageCreated += deleters.DeletePotentialSpam;

        duplicateMessageFilter =  new DuplicateMessageFilter(this, configOptions);
        Client.MessageCreated  += duplicateMessageFilter.MessageCreated;
        duplicateMessageFilter.Start();

        threadKeepAlive = new ThreadKeepAlive(this, configOptions);
        threadKeepAlive.Start();

        CommandsNextConfiguration commandsConfig = new()
        {
            DmHelp                   = true,
            EnableMentionPrefix      = true,
            ServiceProvider          = services,
            StringPrefixes           = new List<string> { "!" },
            UseDefaultCommandHandler = false,
        };
        CommandsNextExtension commands = Client.UseCommandsNext(commandsConfig);
        commands.CommandExecuted += Logging.Logging.CommandExecuted;
        commands.CommandErrored  += Logging.Logging.CommandErrored;
        commands.RegisterConverter(new DateTimeConverter());
        commands.RegisterCommands(Assembly.GetAssembly(typeof(BotMain))!);

        commandHandler = typeof(CommandsNextExtension).GetMethod("HandleCommandsAsync",
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?.CreateDelegate<CommandHandler>(commands)
                ?? throw new Exception("Failed to create delegate for CommandsNextExtension.HandleCommandsAsync!");
    }

    public DiscordGuild OutputGuild => Client.Guilds[config.OutputGuildId];

    public DiscordChannel SpamReportChannel => OutputGuild.Channels[config.SpamReportChannel];

    public DiscordRole MutedRole => OutputGuild.Roles[config.MutedRole];

    public void Dispose()
    {
        Client.Dispose();
        duplicateMessageFilter.Cancel();
        duplicateMessageFilter.Dispose();
        threadKeepAlive.Cancel();
        threadKeepAlive.Dispose();
        services.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Kill() => shutdownRequest.Cancel();

    public async Task<IsModerator> IsUserModerator(DiscordUser user)
    {
        DiscordMember guildUser = await GetMemberFromUser(user);
        return config.ModeratorRoleIds.Overlaps(guildUser.Roles.Select(r => r.Id)) ? IsModerator.Yes : IsModerator.No;
    }

    public async Task<DiscordMember> GetMemberFromUser(DiscordUser user) =>
        user is DiscordMember rgu ? rgu : await OutputGuild.GetMemberAsync(user.Id);

    public async Task<IsExemptFromSpamFilter> IsUserExemptFromSpamFilter(DiscordUser user)
    {
        DiscordMember guildUser = await GetMemberFromUser(user);
        return guildUser.Roles.Any(r => r.Id == config.SpamFilterExemptionRole)
                   ? IsExemptFromSpamFilter.Yes
                   : IsExemptFromSpamFilter.No;
    }

    public async Task MuteUser(DiscordUser user, string reason)
    {
        DiscordMember guildUser = await GetMemberFromUser(user);
        await guildUser.GrantRoleAsync(MutedRole, reason);
    }


    private delegate Task CommandHandler(DiscordClient sender, MessageCreateEventArgs args);
    private readonly CommandHandler commandHandler;
    private Task HandleCommand(DiscordClient sender, MessageCreateEventArgs args)
    {
        if (!config.ProhibitCommandsFromUsers.Contains(args.Author.Id))
        {
            return commandHandler(sender, args);
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

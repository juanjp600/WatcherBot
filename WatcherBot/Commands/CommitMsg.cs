using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using WatcherBot.Utils;
using WatcherBot.Utils.ANSI;
using static WatcherBot.FSharp.CommitMessage;

namespace WatcherBot.Commands;

// ReSharper disable once UnusedType.Global
public class CommitCommandModule : BaseCommandModule
{
    private readonly BotMain botMain;
    private readonly Config.Config config;

    public CommitCommandModule(BotMain bm, IOptions<Config.Config> cfg)
    {
        botMain = bm;
        config  = cfg.Value;
    }

    [Command("commitmsg")]
    [Aliases("c", "commit")]
    [Description("Fetch full commit messages for a specified hashes")]
    public async Task CommitMsg(
        CommandContext context,
        [Description("The hash to retrieve commit message for. " + "Check <#431606297134104577> for stubs.")]
        params string[] hashes)
    {
        using (context.Channel.TriggerTypingAsync())
        {
            context.Client.Logger.LogDebug("Fetching messages...");
            string content = await GetCommitMessages(botMain.GitHubClient, hashes);
            context.Client.Logger.LogDebug("Got responses");
            if (content.Length <= 2000)
            {
                context.Client.Logger.LogDebug("Sending message...");
                await context.RespondAsync(content);
                context.Client.Logger.LogDebug("Sent");
            }
            else
            {
                context.Client.Logger.LogDebug("Content too long for a single message; creating streams for file...");
                await using var memoryStream = content.ToMemoryStream();
                await context.RespondAsync(new DiscordMessageBuilder().WithFile("commits.md", memoryStream));
                context.Client.Logger.LogDebug("Sent");
            }
        }
    }

    [Command("issue")]
    [Aliases("i")]
    public async Task Issue(CommandContext context) =>
        await
            context.RespondAsync("You can open an issue for that!\n<https://github.com/Regalis11/Barotrauma/issues/new?template=bug_report.md>");

    [Command("issue")]
    [Aliases("pr", "pull")]
    [Description("Find a particular issue or pull request")]
    public async Task Issue(CommandContext context, int number)
    {
        Issue issue = await botMain.GitHubClient.Issue.Get("Regalis11", "Barotrauma", number);
        await context.RespondAsync(issue.HtmlUrl);
    }

    [Command("openissues")]
    public async Task OpenIssues(CommandContext context)
    {
        // Get open Unstable issues
        var request = new RepositoryIssueRequest {
            Labels = {"Unstable"},
            State  = ItemStateFilter.Open,
        };

        IReadOnlyList<Issue> issues =
            await botMain.GitHubClient.Issue.GetAllForRepository("Regalis11", "Barotrauma", request);

        // Used to align each line
        int padding = issues.Max(i => i.Number).ToString().Length + 2;

        StringBuilder stringBuilder = new();

        // Colour an issue label based on the config
        string Format(string name)
        {
            ForegroundColour? colour = null;
            Style?            style  = null;

            if (config.Issues.LabelColours.TryGetValue(name, out ForegroundColour c))
            {
                colour = c;
            }
            
            if (config.Issues.EmphasiseLabels.Contains(name))
            {
                style = Style.Bold;
            }

            return name.WithOptionalForegroundColourAndStyle(colour, style);
        }

        void MakeLine(Issue issue)
        {
            string number = $"{issue.Number}.".PadRight(padding).WithForegroundColour(ForegroundColour.Red);
            string labels = issue.Labels
                                 .ExceptBy(config.Issues.HideLabels, l => l.Name, StringComparer.OrdinalIgnoreCase)
                                 .OrderByDescending(l => config.Issues.EmphasiseLabels.Contains(l.Name) ? 1 : 0)
                                 .Select(l => Format(l.Name))
                                 .StringConcat(", ");
            var spaces = new string(' ', padding);

            stringBuilder.Append(number);

            stringBuilder.AppendLine(issue.Title);

            if (!string.IsNullOrWhiteSpace(labels)) { stringBuilder.AppendLine($"{spaces}{labels}"); }

            stringBuilder.Append(spaces);
            stringBuilder.AppendLine(issue.HtmlUrl.WithForegroundColourAndStyle(ForegroundColour.Blue, Style.Bold));
            stringBuilder.AppendLine("");
        }

        IEnumerable<int> scores = issues.Select(i => i.CountLabelWeighting(config.Issues.LabelWeighting));

        issues.Zip(scores)
              .OrderByDescending(pair => pair.Second)
              .ThenBy(pair => pair.First.Number)
              .ForEach(pair => MakeLine(pair.First));

        await using var memoryStream = stringBuilder.ToString().ToMemoryStream();

        DiscordMessageBuilder builder = new DiscordMessageBuilder().WithFile("issues.ansi", memoryStream);

        await context.RespondAsync(builder);
    }
}

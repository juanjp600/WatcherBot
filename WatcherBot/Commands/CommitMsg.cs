using System.IO;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Microsoft.Extensions.Logging;
using Octokit;
using static WatcherBot.FSharp.CommitMessage;
using FileMode = System.IO.FileMode;

namespace WatcherBot.Commands;

// ReSharper disable once UnusedType.Global
public class CommitCommandModule : BaseCommandModule
{
    private readonly BotMain botMain;

    public CommitCommandModule(BotMain bm)
    {
        botMain = bm;
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
                await using var memoryStream = new MemoryStream();
                await using var writer       = new StreamWriter(memoryStream);
                context.Client.Logger.LogDebug("Writing to stream");
                await writer.WriteAsync(content);
                await writer.FlushAsync();
                memoryStream.Position = 0;
                context.Client.Logger.LogDebug("Stream prepared");
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
}

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

    public CommitCommandModule(BotMain bm) => botMain = bm;

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
                context.Client.Logger.LogDebug("Output too long for a message; creating files");
                // Create a temporary directory
                string directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(directory);
                // Save the string to a file there
                string filepath = Path.Combine(directory, "commits.md");
                context.Client.Logger.LogDebug("Temp filepath: {Filepath}", filepath);
                await File.WriteAllTextAsync(filepath, content);
                // Send the file to Discord
                context.Client.Logger.LogDebug("Sending file...");
                await context.RespondAsync(new DiscordMessageBuilder().WithFile(new FileStream(filepath,
                                                                                    FileMode.Open)));
                context.Client.Logger.LogDebug("Sent");
                // Delete the temporary directory
                context.Client.Logger.LogDebug("Deleting {Filepath}", filepath);
                Directory.Delete(directory, true);
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

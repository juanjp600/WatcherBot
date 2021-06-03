using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;

namespace Bot600
{
    public class CommitCommandModule : ModuleBase<SocketCommandContext>
    {
        private readonly BotMain botMain;
        public CommitCommandModule(BotMain bm)
        {
            botMain = bm;
        }

        private Result<Octokit.GitHubCommit> GetCommitMessage(string hash)
        {
            //TODO: make this method async so we can use await instead of Result
            try
            {
                var ghClient = botMain.GitHubClient;
                var commit = ghClient.Repository.Commit.Get("Regalis11", "Barotrauma-development", hash).Result;
                return Result<Octokit.GitHubCommit>.Success(commit);
            }
            catch (AggregateException e)
            {
                return Result<Octokit.GitHubCommit>.Failure($"Error executing !commitmsg: {string.Join(", ", e.InnerExceptions.Select(inner => inner.Message))}");
            }
            catch (Exception e)
            {
                return Result<Octokit.GitHubCommit>.Failure($"Error executing !commitmsg: {e.Message}");
            }
        }

        [Command("commitmsg", RunMode = RunMode.Async)]
        [Summary("Gets a commit message.")]
        [Alias("c", "commit")]
        public async Task CommitMsg2([Summary("The hash or GitHub URL to get the commit message for")] params string[] hashes)
        {
            await Task.Yield();
            using (Context.Channel.EnterTypingState())
            {
                var result =
                    hashes
                        .Select(hash =>
                            // Construct initial Result.
                            string.IsNullOrWhiteSpace(hash)
                                ? Result<string>.Failure("Error executing !commitmsg: empty parameter")
                                : Result<string>.Success(hash)

                                    // Extract hashes from GitHub URLs
                                    .Map(h =>
                                    {
                                        h = Path.TrimEndingDirectorySeparator(h);
                                        if (h.Contains('/')) h = h.Substring(h.LastIndexOf('/') + 1);
                                        hash = h;

                                        return h;
                                    })

                                    // Regex to only match hexadecimal input
                                    .Bind(h =>
                                        Regex.IsMatch(h, @"^[0-9a-fA-F]{5,40}$")
                                            ? Result<string>.Success(h)
                                            : Result<string>.Failure(
                                                "Error executing !commitmsg: argument is invalid")))
                        .ToHashSet(new HashComparer())
                        .Select(hash => hash
                            // Try to get the commit message.
                            .Bind(h => GetCommitMessage(h)
                                    // Format for Discord message.
                                    .Map(commit => $"`{commit.Sha.Substring(0, Math.Min(commit.Sha.Length, 10))}: {commit.Commit.Message}`")))
                        // Turn each Result into a string.
                        .Select(r => r.ToString());

                await ReplyAsync(string.Join("\n", result));
            }
        }
    }
}

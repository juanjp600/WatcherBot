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
        private static ProcessStartInfo ProcessStartInfo => new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Normal,
            WorkingDirectory = "./Barotrauma-development/",
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        private static void Fetch()
        {
            var process = new Process {StartInfo = ProcessStartInfo};
            process.StartInfo.Arguments = "fetch";
            process.Start();
        }

        private static Result<string> GetCommitMessage(string hash)
        {
            hash = Path.TrimEndingDirectorySeparator(hash);
            if (hash.Contains('/')) hash = hash.Substring(hash.LastIndexOf('/') + 1);

            if (!Regex.IsMatch(hash, @"^[0-9a-fA-F]{5,40}$"))
                return Result<string>.Failure("Error executing !commitmsg: argument is invalid");

            var process = new Process {StartInfo = ProcessStartInfo};
            process.StartInfo.Arguments = $"show-branch --no-name {hash}";
            process.Start();
            var output = process.StandardOutput.ReadToEnd();

            return string.IsNullOrWhiteSpace(output)
                ? Result<string>.Failure($"Error executing !commitmsg: could not find commit {hash}")
                : Result<string>.Success(output);
        }

        [Command("commitmsg", RunMode = RunMode.Async)]
        [Summary("Gets a commit message.")]
        [Alias("c", "commit")]
        public async Task CommitMsg([Summary("The hash or GitHub URL to get the commit message for")]
            params string[] hashes)
        {
            await Task.Yield();
            using (Context.Channel.EnterTypingState())
            {
                var result =
                    hashes.Select(hash =>
                            string.IsNullOrWhiteSpace(hash)
                                ? Result<string>.Failure("Error executing !commitmsg: empty parameter")
                                : Result<string>.Success(hash)
                                    .Bind(h =>
                                        Regex.IsMatch(h, @"^[0-9a-fA-F]{5,40}$")
                                            ? Result<string>.Success(h)
                                            : Result<string>.Failure(""))

                                    .Map(h =>
                                    {
                                        h = Path.TrimEndingDirectorySeparator(h);
                                        if (h.Contains('/')) h = h.Substring(h.LastIndexOf('/'));

                                        return h;
                                    })

                                    .Bind(GetCommitMessage)

                                    .OrElseThunk(() =>
                                    {
                                        Fetch();
                                        return GetCommitMessage(hash);
                                    })

                                    .Map(msg => $"`{hash.Substring(0, Math.Min(hash.Length, 10))}: {msg}`"))
                        .Select(r => r.ToString());

                ReplyAsync(string.Join("\n", result));
            }
        }
    }
}

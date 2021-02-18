using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Bot600
{
    public class CommitCommandModule : ModuleBase<SocketCommandContext>
    {
        private static ProcessStartInfo ProcessStartInfo => new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Normal,
            WorkingDirectory = "/home/jlb/Documents/C#/Bot600/",
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        private static void Fetch()
        {
            Process process = new Process {StartInfo = ProcessStartInfo};
            process.StartInfo.Arguments = "fetch";
            process.Start();
        }
        
        private static Result<string> GetCommitMessage(string hash)
        {
            hash = Path.TrimEndingDirectorySeparator(hash);
            if (hash.Contains('/'))
            {
                hash = hash.Substring(hash.LastIndexOf('/') + 1);
            }
            
            if (!Regex.IsMatch(hash, @"^[0-9a-fA-F]{5,40}$"))
            {
                return Result<string>.Failure($"Error executing !commitmsg: argument is invalid");
            }

            Process process = new Process {StartInfo = ProcessStartInfo};
            process.StartInfo.Arguments = $"show-branch --no-name {hash}";
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            
            return string.IsNullOrWhiteSpace(output)
                ? Result<string>.Failure("Error executing !commitmsg: argument is invalid")
                : Result<string>.Success(output);
        }

        [Command("commitmsg", RunMode = RunMode.Async)]
        [Summary("Gets a commit message.")]
        [Alias("c", "commit")]
        public async Task CommitMsg([Remainder][Summary("The hash or GitHub URL to get the commit message for")] string hash = null)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                ReplyAsync("Error executing !commitmsg: expected at least one argument");
                return;
            }
            await Task.Yield();

            hash = Path.TrimEndingDirectorySeparator(hash);
            if (hash.Contains('/'))
            {
                hash = hash.Substring(hash.LastIndexOf('/') + 1);
            }

            if (!Regex.IsMatch(hash, @"^[0-9a-fA-F]{5,40}$"))
            {
                ReplyAsync($"Error executing !commitmsg: argument is invalid");
                return;
            }

            var result =
                GetCommitMessage(hash)
                    .OrElseThunk(() =>
                    {
                        Fetch();
                        return GetCommitMessage(hash);
                    })
                    .Map(msg => $"`{hash.Substring(0, Math.Min(hash.Length, 10))}: {msg}`")
                    .ToString();

            ReplyAsync(result);
        }
    }
}

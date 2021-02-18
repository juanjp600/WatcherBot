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

            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = "./Barotrauma-development/",
                FileName = "git",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            for (int i = 0; i < 2; i++)
            {
                processStartInfo.Arguments = $"show-branch --no-name {hash}";

                Process process = new Process {StartInfo = processStartInfo};
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                if (string.IsNullOrWhiteSpace(output))
                {
                    if (i > 0)
                    {
                        ReplyAsync($"Error executing !commitmsg: argument is invalid");
                        return;
                    }
                    else
                    {
                        processStartInfo.Arguments = "fetch";

                        Console.WriteLine($"Fetching for {hash}...");

                        process = new Process {StartInfo = processStartInfo};
                        process.Start();
                        output = process.StandardOutput.ReadToEnd();

                        Console.WriteLine($"Fetched! Outputting {hash} name...");
                    }
                }
                else
                {
                    Console.WriteLine($"{hash}: {output}");
                    if (hash.Length > 10) { hash = hash.Substring(0, 10); }
                    ReplyAsync($"`{hash} {output}`");
                    return;
                }
            }
        }
    }
}

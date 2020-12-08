using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bot600
{
    public class CommitCommandModule : ModuleBase<SocketCommandContext>
    {
        [Command("c", RunMode = RunMode.Async)]
        [Summary("Gets a commit message.")]
        public async Task C([Remainder][Summary("The hash or GitHub URL to get the commit message for")] string hash = null)
        {
            await CommitMsg(hash);
        }

        [Command("commit", RunMode = RunMode.Async)]
        [Summary("Gets a commit message.")]
        public async Task Commit([Remainder][Summary("The hash or GitHub URL to get the commit message for")] string hash = null)
        {
            await CommitMsg(hash);
        }

        [Command("commitmsg", RunMode = RunMode.Async)]
        [Summary("Gets a commit message.")]
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

            if (!System.Text.RegularExpressions.Regex.IsMatch(hash, @"^[a-zA-Z0-9]+$"))
            {
                ReplyAsync($"Error executing !commitmsg: argument is invalid");
                return;
            }

            Process process = new Process();

            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.WindowStyle = ProcessWindowStyle.Normal;
            processStartInfo.WorkingDirectory = "./Barotrauma-development/";
            processStartInfo.FileName = "git";
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.UseShellExecute = false;

            for (int i = 0; i < 2; i++)
            {
                processStartInfo.Arguments = $"show-branch --no-name {hash}";

                process = new Process();
                process.StartInfo = processStartInfo;
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

                        process = new Process();
                        process.StartInfo = processStartInfo;
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

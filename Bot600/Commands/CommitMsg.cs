using System.IO;
using System.Threading.Tasks;
using Discord.Commands;
using static Bot600.FSharp.CommitMessage;

namespace Bot600
{
    public class CommitCommandModule : ModuleBase<SocketCommandContext>
    {
        private readonly BotMain botMain;

        public CommitCommandModule(BotMain bm)
        {
            botMain = bm;
        }

        [Command("commitmsg", RunMode = RunMode.Async)]
        [Summary("Gets a commit message.")]
        [Alias("c", "commit")]
        public async Task CommitMsg2([Summary("The hash or GitHub URL to get the commit message for")]
                                     params string[] hashes)
        {
            using (Context.Channel.EnterTypingState())
            {
                string content = GetCommitMessages(botMain.GitHubClient, hashes);
                if (content.Length <= 2000)
                {
                    await ReplyAsync(content);
                }
                else
                {
                    // Create a temporary directory
                    string directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    Directory.CreateDirectory(directory);
                    // Save the string to a file there
                    string filepath = Path.Combine(directory, "commits.txt");
                    await File.WriteAllTextAsync(filepath, content);
                    // Send the file to Discord
                    await Context.Channel.SendFileAsync(filepath);
                    // Delete the temporary directory
                    Directory.Delete(directory, true);
                }
            }
        }
    }
}

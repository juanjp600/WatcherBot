using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using Microsoft.Extensions.Logging;

namespace WatcherBot.Logging
{
    public static class Logging
    {
        public static Task CommandExecuted(CommandsNextExtension extension, CommandExecutionEventArgs args)
        {
            extension.Client.Logger.LogInformation("Calling {Command} (invoked by {User})", args.Command.QualifiedName,
                                                   args.Context.User.UsernameWithDiscriminator);
            return Task.CompletedTask;
        }

        public static Task CommandErrored(CommandsNextExtension extension, CommandErrorEventArgs args)
        {
            extension.Client.Logger.LogWarning("Command {Command} (invoked by {User}) failed to execute: {Exception}",
                                               args.Command.QualifiedName, args.Context.User.UsernameWithDiscriminator,
                                               args.Exception);
            return Task.CompletedTask;
        }
    }
}

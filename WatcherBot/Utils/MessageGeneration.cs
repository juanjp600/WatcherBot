using DisCatSharp.Entities;

namespace WatcherBot.Utils;

static class MessageGeneration
{
    public static string QueryableName(this DiscordUser user)
        => user.IsMigrated ? user.Username : user.UsernameWithDiscriminator;
}
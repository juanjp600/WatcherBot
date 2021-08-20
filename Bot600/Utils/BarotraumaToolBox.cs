using System;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;

namespace Bot600.Utils
{
    public static class BarotraumaToolBox
    {
        private static async Task<DiscordDmChannel?> GetDmChannelAsync(this CommandContext context)
        {
            if (context.Channel is DiscordDmChannel dmChannel)
            {
                return dmChannel;
            }

            if (context.Member is not null)
            {
                return await context.Member.CreateDmChannelAsync();
            }

            return null;
        }

        public static async Task RespondDmAsync(this CommandContext context, Action<DiscordMessageBuilder> action)
        {
            DiscordDmChannel? dmChannel = await context.GetDmChannelAsync();
            if (dmChannel is not null)
            {
                await dmChannel.SendMessageAsync(action);
            }
        }

        public static async Task RespondDmAsync(this CommandContext context, DiscordEmbed embed)
        {
            DiscordDmChannel? dmChannel = await context.GetDmChannelAsync();
            if (dmChannel is not null)
            {
                await dmChannel.SendMessageAsync(embed);
            }
        }

        public static async Task RespondDmAsync(this CommandContext context, DiscordMessageBuilder builder)
        {
            DiscordDmChannel? dmChannel = await context.GetDmChannelAsync();
            if (dmChannel is not null)
            {
                await dmChannel.SendMessageAsync(builder);
            }
        }

        public static async Task RespondDmAsync(this CommandContext context, string content)
        {
            DiscordDmChannel? dmChannel = await context.GetDmChannelAsync();
            if (dmChannel is not null)
            {
                await dmChannel.SendMessageAsync(content);
            }
        }

        public static async Task RespondDmAsync(this CommandContext context, string content, DiscordEmbed embed)
        {
            DiscordDmChannel? dmChannel = await context.GetDmChannelAsync();
            if (dmChannel is not null)
            {
                await dmChannel.SendMessageAsync(content, embed);
            }
        }

        public static int CountSubstrings(this string str, string substr)
        {
            var count = 0;
            var index = 0;
            while (true)
            {
                index = str.IndexOf(substr, index, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    break;
                }

                index++;
                count++;
            }

            return count;
        }

        public static bool ToBool(this IsCringe cringe) => cringe == IsCringe.Yes;

        public static IsCringe ToCringe(this bool @bool) => @bool ? IsCringe.Yes : IsCringe.No;
    }
}

using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WatcherBot.Utils;

namespace WatcherBot.Commands;

[EventHandler]
public class CreateSelfRoleMessageCommandModule : BaseCommandModule
{
    private static readonly string BUTTON_IDENTIFIER = "csrm";

    [Event]
    private async Task ComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs args)
    {
        string id = args.Id;
        if (!id.StartsWith(BUTTON_IDENTIFIER))
        {
            return;
        }

        DiscordRole role = args.Guild.GetRole(ulong.Parse(id.Substring(BUTTON_IDENTIFIER.Length)));
        DiscordMember member = await args.Guild.GetMemberAsync(args.User.Id);
        string resp;
        if (member.Roles.Contains(role))
        {
            member.RevokeRoleAsync(role);
            resp = $"You've been removed from the {role.Name} role.";
        }
        else
        {
            member.GrantRoleAsync(role);
            resp = $"You've been added to the {role.Name} role.";
        }

        await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AsEphemeral().WithContent(resp));
    }

    private record JSONRole(ulong Id, string Name, string Description, ButtonStyle Style = 0, string? Emoji = null);

    private record JSONData(JSONRole[] Roles, bool Inline = true, ButtonStyle DefaultButtonStyle = ButtonStyle.Primary, string? Image = null);

    [Command("create-self-role-message")]
    [Description("Creates a message that allows users to assign roles to themself")]
    [RequirePermissionInGuild(Permissions.ManageMessages)]
    [RequireModeratorRoleInGuild]
    public async Task CreateSelfRoleMessage(CommandContext context, [RemainingText] string json)
    {
        JsonSerializerOptions opt = new() {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
        };
        JSONData data = JsonSerializer.Deserialize<JSONData>(json, opt) ?? throw new Exception("Epic deserialization failure");

        DiscordEmbedBuilder builder = new();
        if (data.Image is not null)
        {
            builder.Author.Url = data.Image;
        }

        foreach (var role in data.Roles)
        {
            builder.AddField(role.Name, role.Description, data.Inline);
        }

        var buttons = data.Roles.Select(r => new DiscordButtonComponent(
            r.Style != 0 ? r.Style : data.DefaultButtonStyle,
            BUTTON_IDENTIFIER + r.Id,
            r.Name,
            emoji: r.Emoji is null ? null : new DiscordComponentEmoji(
                context.CommandsNext.ConvertArgument<DiscordEmoji>(r.Emoji, context).Result as DiscordEmoji)));

        DiscordMessageBuilder messageBuilder = new();
        messageBuilder.AddEmbed(builder);
        messageBuilder.AddComponents(buttons);
        await context.Channel.SendMessageAsync(messageBuilder);
    }
}

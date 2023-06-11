using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Enums;
using Microsoft.Extensions.Options;
using WatcherBot.Utils;

namespace WatcherBot.Commands;

public class ChangeDefaultRecipientCommandModule : BaseCommandModule
{
    private readonly Templates templates;

    public ChangeDefaultRecipientCommandModule(IOptions<Config.Config> cfg)
    {
        templates = cfg.Value.Templates;
    }

    [Command("changedefaultrecipient")]
    [Description("Change the default recipient of appeals, i.e. the username that's provided when using !ban_anon")]
    [RequirePermissionInGuild(Permissions.BanMembers)]
    [RequireModeratorRoleInGuild]
    [RequireDmOrOutputGuild]
    public async Task ChangeDefaultRecipient(CommandContext context, [RemainingText] string? newRecipient = null)
    {
        if (string.IsNullOrWhiteSpace(newRecipient))
        {
            await context.Message.Channel.SendMessageAsync($"Current default appeal recipient is `{templates.DefaultAppealRecipient}`.");
            return;
        }
        var prevRecipient = templates.DefaultAppealRecipient;
        templates.DefaultAppealRecipient = newRecipient;
        await context.Message.Channel.SendMessageAsync($"Default appeal recipient has been changed from `{prevRecipient}` to `{newRecipient}`.");
    }
}
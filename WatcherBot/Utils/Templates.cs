using Microsoft.Extensions.Configuration;
using System;

namespace WatcherBot.Utils;

public record Templates(string Ban, string Timeout, string DefaultAppealRecipient)
{
    public string GetAppealRecipients(string otherName, Anonymous anon)
        => anon == Anonymous.Yes || otherName == DefaultAppealRecipient
            ? DefaultAppealRecipient
            : $"{otherName} or {DefaultAppealRecipient}";

    public static Templates FromConfig(Config.Config config)
    {
        var section = config.Configuration.GetSection(nameof(Templates));
        string arrSecToStr(string key) => string.Join("\n", section.GetSection(key).Get<string[]>());
        return new Templates(
            Ban: arrSecToStr(nameof(Ban)),
            Timeout: arrSecToStr(nameof(Timeout)),
            DefaultAppealRecipient: section.GetSection(nameof(DefaultAppealRecipient)).Get<string>());
    }
}

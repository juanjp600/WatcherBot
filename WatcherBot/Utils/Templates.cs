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
        var section = config.Configuration.GetSection("Templates");
        string arrSecToStr(string key) => string.Join("\n", section.GetSection(key).Get<string[]>());
        return new Templates(arrSecToStr("Ban"), arrSecToStr("Timeout"), section.GetSection("DefaultAppeal").Get<string>());
    }
}

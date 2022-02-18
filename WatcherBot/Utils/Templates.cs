using Microsoft.Extensions.Configuration;
using System;

namespace WatcherBot.Utils;

public class Templates
{
    public readonly string Ban;
    public readonly string Timeout;

    private readonly string defaultAppealRecipent;

    private Templates(string ban, string timeout, string defaultAppealRecipent) {
        Ban = ban;
        Timeout = timeout;
        this.defaultAppealRecipent = defaultAppealRecipent;
    }

    public string GetAppealRecipients(string otherName, Anonymous anon) {
        if (anon == Anonymous.Yes || otherName == defaultAppealRecipent) {
            return defaultAppealRecipent;
        } else {
            return $"{otherName} or {defaultAppealRecipent}";
        }
    }

    public static Templates FromConfig(Config.Config config)
    {
        var section = config.Configuration.GetSection("Templates");
        string arrSecToStr(string key) => string.Join("\n", section.GetSection(key).Get<string[]>());
        return new Templates(arrSecToStr("Ban"), arrSecToStr("Timeout"), section.GetSection("DefaultAppeal").Get<string>());
    }
}

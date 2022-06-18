using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Converters;
using DisCatSharp.Entities;

namespace WatcherBot.Utils;

public class DateTimeConverter : IArgumentConverter<DateTime>
{
    public async Task<Optional<DateTime>> ConvertAsync(string value, CommandContext ctx)
    {
        Match match = await Task.Run(() => Regex.Match(value.ToLower(), @"^(\d+)([a-z]+)$"));
        if (!match.Success || !int.TryParse(match.Groups[1].ValueSpan, out int count))
        {
            return Optional.FromNoValue<DateTime>();
        }

        DateTime ret;
        switch (match.Groups[2].Value)
        {
            case "ms":
                ret = DateTime.Now.AddMilliseconds(count);
                break;
            case "s":
                ret = DateTime.Now.AddSeconds(count);
                break;
            case "min":
                ret = DateTime.Now.AddMinutes(count);
                break;
            case "h":
                ret = DateTime.Now.AddHours(count);
                break;
            case "d":
                ret = DateTime.Now.AddDays(count);
                break;
            case "w":
                ret = DateTime.Now.AddDays(count * 7);
                break;
            case "mth":
            case "mo":
                ret = DateTime.Now.AddMonths(count);
                break;
            case "y":
            case "a":
                ret = DateTime.Now.AddYears(count);
                break;
            default:
                return Optional.FromNoValue<DateTime>();
        }

        return Optional.FromValue(ret);
    }
}

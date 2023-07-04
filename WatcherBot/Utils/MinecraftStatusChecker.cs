using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using DisCatSharp.Entities;
using MCStatus;

namespace WatcherBot.Utils;

public record MinecraftServer(string Host, ushort? Port, ulong ChannelId, string OnlineFormat, string Offline);

public class MinecraftStatusChecker : LoopingTask
{
    public MinecraftStatusChecker(BotMain botMain, Config.Config config) : base(botMain, config)
    {
        LoopFrequency = config.MinecraftCheckInterval;
    }

    protected override TimeSpan LoopFrequency { get; }

    protected override async Task LoopWork()
    {
        try
        {
            await Task.WhenAll(Config.MinecraftServers.Select(async server => {
                string str;
                try
                {
                    var status =
                        await ServerListClient.GetStatusAsync(server.Host, server.Port.GetValueOrDefault(25565));
                    str = string.Format(server.OnlineFormat, status.Description,
                        status.Players.Online, status.Players.Max, status.Version.Name);
                }
                catch (Exception)
                {
                    str = server.Offline;
                }

                var channel = await BotMain.Client.TryGetChannelAsync(server.ChannelId);
                if (channel is not null && channel.Name != str)
                    await channel.ModifyAsync(x => x.Name = str);
            }));
        }
        catch (Exception)
        {
            // ignored
        }
    }
}

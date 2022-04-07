using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WatcherBot.Utils;

public abstract class LoopingTask : IDisposable
{
    protected readonly BotMain BotMain;
    protected readonly Config.Config Config;
    protected readonly ILogger Logger;

    public LoopingTask(BotMain botMain, Config.Config config)
    {
        BotMain = botMain;
        Config  = config;
        Logger  = botMain.Client.Logger;
    }

    public CancellationTokenSource CancellationTokenSource { get; } = new();
    protected abstract TimeSpan LoopFrequency { get; }

    public Task? Loop { get; private set; }

    public virtual void Dispose()
    {
        Cancel();
        Loop?.Dispose();
        Loop = null;
        GC.SuppressFinalize(this);
    }

    public void Start()
    {
        if (CancellationTokenSource.IsCancellationRequested) { return; }

        Loop = Task.Run(MainLoop);
    }

    public void Cancel() => CancellationTokenSource.Cancel();

    protected abstract Task LoopWork();

    private async Task MainLoop()
    {
        PeriodicTimer timer = new(LoopFrequency);
        while (!CancellationTokenSource.IsCancellationRequested)
        {
            await timer.WaitForNextTickAsync();
            await LoopWork();
        }
    }
}

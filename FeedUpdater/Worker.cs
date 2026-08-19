using Aspirgregator.Abstractions;
using System.Diagnostics;

namespace FeedUpdater;

public class Worker(ILogger<Worker> logger, IGrainFactory grainFactory) : BackgroundService
{
    static TimeSpan _updateInterval = TimeSpan.FromMinutes(5);
    //static TimeSpan _updateInterval = TimeSpan.FromSeconds(15);

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        WorkerLog.Starting(logger, _updateInterval);

        var sourceLibraryGrain = grainFactory.GetGrain<ISourceLibraryGrain>(Guid.Empty);
        var sources = await sourceLibraryGrain.GetSourcesAsync();

        if(!sources.Any())
        {
            using var fileStream = File.OpenRead("sample_rss_feeds.txt");
            using var reader = new StreamReader(fileStream);
            var line = await reader.ReadLineAsync();
            while(!string.IsNullOrEmpty(line))
            {
                var newSourceGrain = 
                    await grainFactory.GetGrain<ISourceLibraryGrain>(Guid.Empty)
                                      .CreateSource(line);

                line = await reader.ReadLineAsync();
            }
        }

        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        WorkerLog.Stopping(logger);
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var stopwatch = Stopwatch.StartNew();
            var sourceLibraryGrain = grainFactory.GetGrain<ISourceLibraryGrain>(Guid.Empty);
            var sources = (await sourceLibraryGrain.GetSourcesAsync()).ToList();

            var succeeded = 0;
            var failed = 0;

            foreach (var source in sources)
            {
                try
                {
                    var sourceGrain = await sourceLibraryGrain.GetSourceAsync(source.Endpoint);
                    if(sourceGrain is not null)
                    {
                        await sourceGrain.UpdateSourceAsync(source);
                        succeeded++;
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    WorkerLog.SourceUpdateFailed(logger, ex, source.Endpoint);
                }
            }

            stopwatch.Stop();
            WorkerLog.CycleCompleted(logger, sources.Count, succeeded, failed, stopwatch.ElapsedMilliseconds);

            try
            {
                await Task.Delay(_updateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                WorkerLog.CancellationRequested(logger);
            }
        }
    }
}

internal static partial class WorkerLog
{
    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "FeedUpdater worker starting with update interval {UpdateInterval}")]
    public static partial void Starting(ILogger logger, TimeSpan updateInterval);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "FeedUpdater worker stopping")]
    public static partial void Stopping(ILogger logger);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Update cycle completed: {SourceCount} sources considered, {SucceededCount} succeeded, {FailedCount} failed, in {ElapsedMilliseconds}ms")]
    public static partial void CycleCompleted(ILogger logger, int sourceCount, int succeededCount, int failedCount, long elapsedMilliseconds);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Warning, Message = "Failed to update source {SourceEndpoint}")]
    public static partial void SourceUpdateFailed(ILogger logger, Exception exception, string sourceEndpoint);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "FeedUpdater worker cancellation requested during delay; shutting down cleanly")]
    public static partial void CancellationRequested(ILogger logger);
}

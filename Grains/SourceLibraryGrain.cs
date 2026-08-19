using Aspirgregator.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aspiregregator.Frontend.Grains;

public class SourceLibraryGrain(
    [PersistentState("FeedSourceLibrary", storageName: "FeedSourceLibrary")]
    IPersistentState<List<SourceItem>> sources,
    ILogger<SourceLibraryGrain> logger) : Grain, ISourceLibraryGrain
{
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        SourceLibraryGrainLog.Activating(logger, this.GetPrimaryKeyString());
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        SourceLibraryGrainLog.Deactivating(logger, this.GetPrimaryKeyString());
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task<IEnumerable<SourceItem>> GetSourcesAsync()
    {
        var tmp = new List<SourceItem>();
        await sources.ReadStateAsync();
        foreach (var source in sources.State)
        {
            tmp.Add((await GrainFactory.GetGrain<ISourceGrain>(source.Endpoint).GetSourceAsync()));
        }
        return tmp.AsEnumerable();
    }

    public async Task<ISourceGrain?> CreateSource(string endpoint)
    {
        try
        {
            sources.State.Add(new SourceItem { Endpoint = endpoint, Name = "Untitled" });
            await sources.WriteStateAsync();
            var sourceGrain = await GetSourceAsync(endpoint);
            var source = await sourceGrain!.GetSourceAsync();
            source = await sourceGrain.UpdateSourceAsync(source);
            SourceLibraryGrainLog.SourceAdded(logger, endpoint, sources.State.Count);
            return sourceGrain;
        }
        catch (Exception ex)
        {
            SourceLibraryGrainLog.SourceMutationFailed(logger, ex, endpoint);
            throw;
        }
    }

    public async Task<ISourceGrain?> GetSourceAsync(string endpoint)
    {
        await sources.ReadStateAsync();
        return sources.State.Any(x => x.Endpoint == endpoint)
                ? GrainFactory.GetGrain<ISourceGrain?>(endpoint)
                : null;
    }

    public async Task RemoveSourceAsync(SourceItem item)
    {
        sources.State.RemoveAll(x => x.Endpoint.Equals(item.Endpoint));
        await sources.WriteStateAsync();
        SourceLibraryGrainLog.SourceRemoved(logger, item.Endpoint, sources.State.Count);
    }
}

internal static partial class SourceLibraryGrainLog
{
    [LoggerMessage(EventId = 1100, Level = LogLevel.Information, Message = "Activating source library grain {GrainKey}")]
    public static partial void Activating(ILogger logger, string grainKey);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Information, Message = "Deactivating source library grain {GrainKey}")]
    public static partial void Deactivating(ILogger logger, string grainKey);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Information, Message = "Added source {SourceEndpoint}, library now has {SourceCount} sources")]
    public static partial void SourceAdded(ILogger logger, string sourceEndpoint, int sourceCount);

    [LoggerMessage(EventId = 1103, Level = LogLevel.Information, Message = "Removed source {SourceEndpoint}, library now has {SourceCount} sources")]
    public static partial void SourceRemoved(ILogger logger, string sourceEndpoint, int sourceCount);

    [LoggerMessage(EventId = 1104, Level = LogLevel.Error, Message = "Failed to mutate source library for {SourceEndpoint}")]
    public static partial void SourceMutationFailed(ILogger logger, Exception exception, string sourceEndpoint);
}

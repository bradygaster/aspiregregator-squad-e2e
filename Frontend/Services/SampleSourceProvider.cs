using Aspirgregator.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aspiregregator.Frontend.Services;

public sealed class SampleSourceProvider(IGrainFactory grainFactory, AppState appState, ILogger<SampleSourceProvider> logger) : ISourceProvider
{
    public async Task<SourceItem?> GetSourceItemAsync(string endpoint)
    {
        if (grainFactory.GetGrain<ISourceLibraryGrain>(Guid.Empty)
                        .GetSourceAsync(endpoint) is not null)
        {
            return await grainFactory.GetGrain<ISourceGrain>(endpoint)
                                     .GetSourceAsync();
        }

        return null;
    }

    public async Task<IEnumerable<SourceItem>> GetSourcesAsync()
      => (await grainFactory.GetGrain<ISourceLibraryGrain>(Guid.Empty)
                            .GetSourcesAsync())
                                .OrderBy(x => x.Name)
                                .AsEnumerable();


    public async Task SaveSourceItemAsync(SourceItem item)
    {
        try
        {
            await grainFactory.GetGrain<ISourceLibraryGrain>(Guid.Empty)
                              .CreateSource(item.Endpoint);

            SampleSourceProviderLog.SourceSaved(logger, item.Endpoint);
            appState.AppStateChanged();
        }
        catch (Exception ex)
        {
            SampleSourceProviderLog.SourceSaveFailed(logger, ex, item.Endpoint);
            throw;
        }
    }

    public async Task<SourceItem> UpdateAsync(SourceItem source)
    {
        source = await grainFactory.GetGrain<ISourceGrain>(source.Endpoint)
                                   .UpdateSourceAsync(source);

        appState.AppStateChanged();

        return source;
    }

    public async Task RemoveSourceAsync(SourceItem item)
    {
        await grainFactory.GetGrain<ISourceLibraryGrain>(Guid.Empty).RemoveSourceAsync(item);

        SampleSourceProviderLog.SourceRemoved(logger, item.Endpoint);
        appState.AppStateChanged();
    }
}

internal static partial class SampleSourceProviderLog
{
    [LoggerMessage(EventId = 3100, Level = LogLevel.Information, Message = "Saved source {SourceEndpoint}")]
    public static partial void SourceSaved(ILogger logger, string sourceEndpoint);

    [LoggerMessage(EventId = 3101, Level = LogLevel.Warning, Message = "Failed to save source {SourceEndpoint}")]
    public static partial void SourceSaveFailed(ILogger logger, Exception exception, string sourceEndpoint);

    [LoggerMessage(EventId = 3102, Level = LogLevel.Information, Message = "Removed source {SourceEndpoint}")]
    public static partial void SourceRemoved(ILogger logger, string sourceEndpoint);
}

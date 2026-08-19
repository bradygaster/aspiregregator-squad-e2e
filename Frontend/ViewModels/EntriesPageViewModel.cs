using Microsoft.Extensions.Logging;

namespace Aspiregregator.Frontend.ViewModels;

public class EntriesPageViewModel(ISourceProvider sourceProvider, ILogger<EntriesPageViewModel> logger)
{
    public SourceItem? SelectedSource { get; set; }

    internal async Task SelectSource(string sourceSlug)
    {
        try
        {
            var sources = await sourceProvider.GetSourcesAsync();
            SelectedSource = sources.FirstOrDefault(x => x.GetSlug() == sourceSlug);

            if (SelectedSource is null)
            {
                EntriesPageViewModelLog.SourceNotFound(logger, sourceSlug);
            }
        }
        catch (Exception ex)
        {
            EntriesPageViewModelLog.LoadFailed(logger, ex, sourceSlug);
        }
    }
}

internal static partial class EntriesPageViewModelLog
{
    [LoggerMessage(EventId = 3200, Level = LogLevel.Warning, Message = "No source found for slug {SourceSlug}")]
    public static partial void SourceNotFound(ILogger logger, string sourceSlug);

    [LoggerMessage(EventId = 3201, Level = LogLevel.Warning, Message = "Failed to load source for slug {SourceSlug}")]
    public static partial void LoadFailed(ILogger logger, Exception exception, string sourceSlug);
}

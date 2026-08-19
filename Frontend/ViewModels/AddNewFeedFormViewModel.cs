using Aspiregregator.Frontend.Services;
using Microsoft.Extensions.Logging;

namespace Aspiregregator.Frontend.ViewModels;

public class AddNewFeedFormViewModel(ISourceProvider sourceProvider,
    AppState appState,
    ILogger<AddNewFeedFormViewModel> logger)
{
    public string FeedUri { get; set; } = string.Empty;

    public async Task HandleSubmit()
    {
        if (IsValidUrl())
        {
            var newSource = new SourceItem { Endpoint = FeedUri, Name = "Untitled" };
            await sourceProvider.SaveSourceItemAsync(newSource);
            await sourceProvider.UpdateAsync(newSource);

            AddNewFeedFormViewModelLog.FeedAccepted(logger, FeedUri);

            FeedUri = string.Empty;

            appState.AppStateChanged();
        }
        else
        {
            AddNewFeedFormViewModelLog.FeedRejected(logger, FeedUri, GetValidationReason());
        }
    }

    public bool IsValidUrl()
    {
        if (string.IsNullOrWhiteSpace(FeedUri))
        {
            return false;
        }

        if (Uri.TryCreate(FeedUri, UriKind.Absolute, out Uri? uriResult) &&
            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
        {
            return true;
        }

        return false;
    }

    private string GetValidationReason()
    {
        if (string.IsNullOrWhiteSpace(FeedUri))
        {
            return "empty URL";
        }

        if (!Uri.TryCreate(FeedUri, UriKind.Absolute, out Uri? uriResult))
        {
            return "not an absolute URL";
        }

        if (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps)
        {
            return "unsupported scheme";
        }

        return "unknown";
    }
}

internal static partial class AddNewFeedFormViewModelLog
{
    [LoggerMessage(EventId = 3300, Level = LogLevel.Information, Message = "Feed submission accepted for {FeedUri}")]
    public static partial void FeedAccepted(ILogger logger, string feedUri);

    [LoggerMessage(EventId = 3301, Level = LogLevel.Warning, Message = "Feed submission rejected for {FeedUri}: {ValidationReason}")]
    public static partial void FeedRejected(ILogger logger, string feedUri, string validationReason);
}

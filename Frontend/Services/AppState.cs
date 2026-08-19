using Microsoft.Extensions.Logging;

namespace Aspiregregator.Frontend.Services;

public sealed class AppState(ILogger<AppState> logger)
{
    public event Action? StateChanged;

    internal void AppStateChanged()
    {
        AppStateLog.StateChanged(logger, StateChanged?.GetInvocationList().Length ?? 0);
        StateChanged?.Invoke();
    }
}

internal static partial class AppStateLog
{
    [LoggerMessage(EventId = 3000, Level = LogLevel.Information, Message = "AppState changed, notifying {SubscriberCount} subscriber(s)")]
    public static partial void StateChanged(ILogger logger, int subscriberCount);
}

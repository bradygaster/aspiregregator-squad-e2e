using Aspirgregator.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aspiregregator.Frontend.Grains;

public class SavedArticlesGrain(
    [PersistentState("SavedArticles", storageName: "FeedSource")]
    IPersistentState<List<EntryItem>> articles,
    ILogger<SavedArticlesGrain> logger) : Grain, ISavedArticlesGrain
{
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        SavedArticlesGrainLog.Activating(logger, this.GetPrimaryKeyString());
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        SavedArticlesGrainLog.Deactivating(logger, this.GetPrimaryKeyString());
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task<IEnumerable<EntryItem>> GetSavedArticlesAsync()
    {
        await articles.ReadStateAsync();
        return articles.State.AsEnumerable();
    }

    public async Task SaveArticleAsync(EntryItem item)
    {
        try
        {
            await articles.ReadStateAsync();
            if (!articles.State.Any(x => x.Link == item.Link))
            {
                articles.State.Add(item);
                await articles.WriteStateAsync();
                SavedArticlesGrainLog.ArticleSaved(logger, item.Link, articles.State.Count);
            }
        }
        catch (Exception ex)
        {
            SavedArticlesGrainLog.ArticleMutationFailed(logger, ex, item.Link);
            throw;
        }
    }

    public async Task RemoveSavedArticleAsync(string link)
    {
        articles.State.RemoveAll(x => x.Link.Equals(link));
        await articles.WriteStateAsync();
        SavedArticlesGrainLog.ArticleRemoved(logger, link, articles.State.Count);
    }

    public async Task<bool> IsSavedAsync(string link)
    {
        await articles.ReadStateAsync();
        return articles.State.Any(x => x.Link == link);
    }
}

internal static partial class SavedArticlesGrainLog
{
    [LoggerMessage(EventId = 1200, Level = LogLevel.Information, Message = "Activating saved articles grain {GrainKey}")]
    public static partial void Activating(ILogger logger, string grainKey);

    [LoggerMessage(EventId = 1201, Level = LogLevel.Information, Message = "Deactivating saved articles grain {GrainKey}")]
    public static partial void Deactivating(ILogger logger, string grainKey);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Information, Message = "Saved article {ArticleLink}, saved list now has {ArticleCount} articles")]
    public static partial void ArticleSaved(ILogger logger, string articleLink, int articleCount);

    [LoggerMessage(EventId = 1203, Level = LogLevel.Information, Message = "Removed saved article {ArticleLink}, saved list now has {ArticleCount} articles")]
    public static partial void ArticleRemoved(ILogger logger, string articleLink, int articleCount);

    [LoggerMessage(EventId = 1204, Level = LogLevel.Error, Message = "Failed to mutate saved articles for {ArticleLink}")]
    public static partial void ArticleMutationFailed(ILogger logger, Exception exception, string articleLink);
}

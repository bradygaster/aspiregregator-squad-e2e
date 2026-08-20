using Aspiregregator;

namespace Aspirgregator.Abstractions;

[Alias("Aspirgregator.Abstractions.ISavedArticlesGrain")]
public interface ISavedArticlesGrain : IGrainWithGuidKey
{
    Task<IEnumerable<EntryItem>> GetSavedArticlesAsync();
    Task SaveArticleAsync(EntryItem item);
    Task RemoveSavedArticleAsync(string link);
    Task<bool> IsSavedAsync(string link);
}

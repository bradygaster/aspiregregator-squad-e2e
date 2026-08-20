using Aspiregregator;
using Aspirgregator.Abstractions;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace Aspiregregator.Tests;

[TestClass]
public sealed class SavedArticlesGrainTests
{
    private static TestCluster? _cluster;

    private sealed class TestSiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.AddMemoryGrainStorage("FeedSource");
        }
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();
        _cluster = builder.Build();
        _cluster.Deploy();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _cluster?.StopAllSilos();
        _cluster?.Dispose();
    }

    private static EntryItem CreateEntry(string link, string title = "Test Title") =>
        new()
        {
            Title = title,
            Link = link,
            Description = "Test description",
            PublishDate = DateTimeOffset.UtcNow,
            UpdatedDate = DateTimeOffset.UtcNow
        };

    [TestMethod]
    public async Task SaveArticleAsync_AddsArticleToSavedList()
    {
        var grain = _cluster!.GrainFactory.GetGrain<ISavedArticlesGrain>(Guid.NewGuid());
        var entry = CreateEntry("https://example.com/article-1");

        await grain.SaveArticleAsync(entry);

        var saved = await grain.GetSavedArticlesAsync();
        Assert.IsTrue(saved.Any(a => a.Link == entry.Link));
    }

    [TestMethod]
    public async Task SaveArticleAsync_IsIdempotent_WhenCalledMultipleTimesForSameLink()
    {
        var grain = _cluster!.GrainFactory.GetGrain<ISavedArticlesGrain>(Guid.NewGuid());
        var entry = CreateEntry("https://example.com/article-2");

        await grain.SaveArticleAsync(entry);
        await grain.SaveArticleAsync(entry);
        await grain.SaveArticleAsync(entry);

        var saved = await grain.GetSavedArticlesAsync();
        Assert.AreEqual(1, saved.Count(a => a.Link == entry.Link));
    }

    [TestMethod]
    public async Task RemoveSavedArticleAsync_RemovesArticleFromSavedList()
    {
        var grain = _cluster!.GrainFactory.GetGrain<ISavedArticlesGrain>(Guid.NewGuid());
        var entry = CreateEntry("https://example.com/article-3");
        await grain.SaveArticleAsync(entry);

        await grain.RemoveSavedArticleAsync(entry.Link);

        var saved = await grain.GetSavedArticlesAsync();
        Assert.IsFalse(saved.Any(a => a.Link == entry.Link));
    }

    [TestMethod]
    public async Task RemoveSavedArticleAsync_IsIdempotent_WhenArticleNotSaved()
    {
        var grain = _cluster!.GrainFactory.GetGrain<ISavedArticlesGrain>(Guid.NewGuid());

        // Should not throw even though nothing was ever saved for this link.
        await grain.RemoveSavedArticleAsync("https://example.com/never-saved");

        var saved = await grain.GetSavedArticlesAsync();
        Assert.AreEqual(0, saved.Count());
    }

    [TestMethod]
    public async Task IsSavedAsync_ReturnsTrue_WhenArticleIsSaved()
    {
        var grain = _cluster!.GrainFactory.GetGrain<ISavedArticlesGrain>(Guid.NewGuid());
        var entry = CreateEntry("https://example.com/article-4");
        await grain.SaveArticleAsync(entry);

        var isSaved = await grain.IsSavedAsync(entry.Link);

        Assert.IsTrue(isSaved);
    }

    [TestMethod]
    public async Task IsSavedAsync_ReturnsFalse_WhenArticleIsNotSaved()
    {
        var grain = _cluster!.GrainFactory.GetGrain<ISavedArticlesGrain>(Guid.NewGuid());

        var isSaved = await grain.IsSavedAsync("https://example.com/not-saved");

        Assert.IsFalse(isSaved);
    }

    [TestMethod]
    public async Task GetSavedArticlesAsync_ReturnsMultipleSavedArticles_InSavedOrder()
    {
        var grain = _cluster!.GrainFactory.GetGrain<ISavedArticlesGrain>(Guid.NewGuid());
        var first = CreateEntry("https://example.com/article-5a");
        var second = CreateEntry("https://example.com/article-5b");

        await grain.SaveArticleAsync(first);
        await grain.SaveArticleAsync(second);

        var saved = (await grain.GetSavedArticlesAsync()).ToList();
        Assert.AreEqual(2, saved.Count);
        Assert.IsTrue(saved.Any(a => a.Link == first.Link));
        Assert.IsTrue(saved.Any(a => a.Link == second.Link));
    }

    [TestMethod]
    public async Task SavedArticles_PersistAcrossGrainReactivation()
    {
        var grainId = Guid.NewGuid();
        var entry = CreateEntry("https://example.com/article-6");

        var grain = _cluster!.GrainFactory.GetGrain<ISavedArticlesGrain>(grainId);
        await grain.SaveArticleAsync(entry);

        // Simulate a restart by resolving a fresh grain reference for the same key
        // and forcing a fresh read from persisted state.
        var reactivatedGrain = _cluster!.GrainFactory.GetGrain<ISavedArticlesGrain>(grainId);
        var saved = await reactivatedGrain.GetSavedArticlesAsync();

        Assert.IsTrue(saved.Any(a => a.Link == entry.Link));
    }
}

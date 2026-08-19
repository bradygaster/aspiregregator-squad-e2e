using Aspirgregator.Abstractions;
using System.Diagnostics;
using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using Microsoft.Extensions.Logging;
using SimpleRssReader = SimpleFeedReader.FeedReader;

namespace Aspiregregator.Frontend.Grains;

public class SourceGrain(
    [PersistentState("FeedSource", storageName: "FeedSource")]
    IPersistentState<SourceItem> source,
    ILogger<SourceGrain> logger) : Grain, ISourceGrain
{
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var grainKey = this.GetPrimaryKeyString();
        source.State.Endpoint = grainKey;
        SourceGrainLog.Activating(logger, grainKey);

        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        SourceGrainLog.Deactivating(logger, this.GetPrimaryKeyString());
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task<IEnumerable<EntryItem>> GetRecentEntries(int pageSize = 10)
    {
        await source.ReadStateAsync();

        return source.State
                        .MostRecentItems
                            .OrderByDescending(x => x.UpdatedDate)
                            .Take(pageSize)
                                .ToList();
    }

    public Task<SourceItem> GetSourceAsync()
        => Task.FromResult(source.State);

    public async Task<SourceItem> UpdateSourceAsync(SourceItem item)
    {
        var stopwatch = Stopwatch.StartNew();
        SourceGrainLog.FetchingSource(logger, item.Endpoint);

        var retrieveTask = Task.Run<List<EntryItem>>(() =>
        {
            var reader = new SimpleRssReader();
            var feedItems = reader.RetrieveFeed(item.Endpoint);

            return
            [
              ..feedItems.Select(x => new EntryItem
                {
                  Title = x.Title,
                  Description = x.Summary,
                  Link = x.Uri.AbsoluteUri,
                  PublishDate = x.PublishDate,
                  UpdatedDate = x.LastUpdatedDate,
                  Image = x.Images?.FirstOrDefault(),
                  Source = item
                })
            ];
        });

        var getFeedTask = FeedReader.ReadAsync(item.Endpoint);

        try
        {
            await Task.WhenAll(retrieveTask, getFeedTask);

            item.MostRecentItems = await retrieveTask;

            var feed = await getFeedTask;
            item.Name = feed.Title;

            source.State = feed.Type switch
            {
                FeedType.MediaRss => WithMediaRssImages(item, feed),
                FeedType.Rss_2_0 => WithRss20Images(item, feed),
                _ => item
            };

            source.State.LastUpdate = DateTimeOffset.UtcNow;

            await source.WriteStateAsync();

            stopwatch.Stop();
            SourceGrainLog.FetchSucceeded(logger, item.Endpoint, item.MostRecentItems.Count, stopwatch.ElapsedMilliseconds);
            SourceGrainLog.StateUpdated(logger, item.Endpoint, source.State.MostRecentItems.Count, source.State.LastUpdate);

            return item;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            SourceGrainLog.FetchFailed(logger, ex, item.Endpoint, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static SourceItem WithRss20Images(SourceItem source, Feed feed)
    {
        foreach (var i in feed.SpecificFeed.Items.Cast<Rss20FeedItem>())
        {
            if (i.Enclosure is not { } enclosure)
            {
                continue;
            }

            var entry = source.MostRecentItems.FirstOrDefault(mri => mri.Link == i.Link);
            if (entry is null)
            {
                continue;
            }

            if (enclosure.Url is not null)
                entry.Image = new(enclosure.Url);
        }

        return source;
    }

    private static SourceItem WithMediaRssImages(SourceItem source, Feed feed)
    {
        foreach (var i in feed.SpecificFeed.Items.Cast<MediaRssFeedItem>())
        {
            if (i.Media.FirstOrDefault() is not { } media)
            {
                continue;
            }

            var entry = source.MostRecentItems.FirstOrDefault(mri => mri.Link == i.Link);
            if (entry is null)
            {
                continue;
            }

            entry.Image = new(media.Url);
        }

        return source;
    }
}

internal static partial class SourceGrainLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Activating source grain {GrainKey}")]
    public static partial void Activating(ILogger logger, string grainKey);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Deactivating source grain {GrainKey}")]
    public static partial void Deactivating(ILogger logger, string grainKey);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Fetching source feed {SourceEndpoint}")]
    public static partial void FetchingSource(ILogger logger, string sourceEndpoint);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "Fetched source feed {SourceEndpoint} with {ItemCount} items in {ElapsedMilliseconds}ms")]
    public static partial void FetchSucceeded(ILogger logger, string sourceEndpoint, int itemCount, long elapsedMilliseconds);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Information, Message = "Persisted source state for {SourceEndpoint} with {ItemCount} items at {LastUpdate}")]
    public static partial void StateUpdated(ILogger logger, string sourceEndpoint, int itemCount, DateTimeOffset lastUpdate);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Error, Message = "Failed to fetch source feed {SourceEndpoint} after {ElapsedMilliseconds}ms")]
    public static partial void FetchFailed(ILogger logger, Exception exception, string sourceEndpoint, long elapsedMilliseconds);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using Matterhook.NET.MatterhookClient;
using ReverseMarkdown;

namespace Matterfeed.NET
{
    internal static class RssFeedReader
    {
        internal static async Task PeriodicRssAsync(TimeSpan interval, List<RssFeed> rssFeeds)
        {
            while (true)
            {
                foreach (var feed in rssFeeds)
                {
                    var sbOut = new StringBuilder();
                    sbOut.Append($"\n{DateTime.Now}\nFetching RSS URL: {feed.Url}");

                    Feed rssFeed;
                    try
                    {
                        rssFeed = await FeedReader.ReadAsync(feed.Url);
                    }
                    catch (Exception e)
                    {
                        sbOut.Append($"\n Unable to get feed. Exception: {e.Message}");
                        Console.WriteLine(sbOut.ToString());
                        continue;
                    }

                    switch (rssFeed.Type)
                    {
                        case FeedType.Atom:
                            sbOut.Append(await ProcessAtomFeed((AtomFeed)rssFeed.SpecificFeed, feed).ConfigureAwait(false));
                            break;
                        case FeedType.Rss:
                            Console.WriteLine("FeedType: RSS");
                            break;
                        case FeedType.Rss_2_0:
                            sbOut.Append(await ProcessRss20Feed((Rss20Feed)rssFeed.SpecificFeed, feed).ConfigureAwait(false));
                            break;
                        case FeedType.Rss_0_91:
                            Console.WriteLine("FeedType: RSS 0.91");
                            break;
                        case FeedType.Rss_0_92:
                            Console.WriteLine("FeedType: RSS 0.92");
                            break;
                        case FeedType.Rss_1_0:
                            Console.WriteLine("FeedType: RSS 1.0");
                            break;
                        default:
                            Console.WriteLine("FeedType: Unknown");
                            break;
                    }

                    Console.WriteLine(sbOut.ToString());
                }
                Program.SaveConfigSection(rssFeeds);
                await Task.Delay(interval).ConfigureAwait(false);
            }

        }

        private static async Task<string> ProcessRss20Feed(Rss20Feed feed, RssFeed rssFeed)
        {
            var sbRet = new StringBuilder();
            sbRet.Append($"\nFeed Type: Rss 2.0\nFeed Title: {feed.Title}\nGenerator: {feed.Generator}");

            var itemCount = feed.Items.Count;
            var procCount = 0;

            while (feed.Items.Any())
            {
                var rss20FeedItem = (Rss20FeedItem)feed.Items.Last();

                if (rss20FeedItem.PublishingDate <= rssFeed.LastProcessedItem || rss20FeedItem.PublishingDate == null)
                {
                    feed.Items.Remove(rss20FeedItem);
                }
                else
                {
                    var converter = new Converter();

                    var message = new MattermostMessage
                    {
                        Channel = rssFeed.BotChannelOverride == ""? null :rssFeed.BotChannelOverride,
                        Username = rssFeed.BotNameOverride==""? null: rssFeed.BotNameOverride,
                        IconUrl = rssFeed.BotImageOverride == "" ? null : new Uri(rssFeed.BotImageOverride),
                        Attachments = new List<MattermostAttachment>
                        {
                            new MattermostAttachment
                            {
                                Pretext = rssFeed.FeedPretext,
                                Title = rss20FeedItem.Title ?? "",
                                TitleLink = rss20FeedItem.Link == null ? null : new Uri(rss20FeedItem.Link),
                                Text = converter.Convert(rssFeed.IncludeContent
                                    ? rss20FeedItem.Content ?? rss20FeedItem.Description ?? ""
                                    : rss20FeedItem.Description ?? ""),
                                AuthorName = rss20FeedItem.Author
                            }
                        }
                    };


                    try
                    {
                        await Program.PostToMattermost(message);
                        
                        rssFeed.LastProcessedItem = rss20FeedItem.PublishingDate;
                        procCount++;
                        feed.Items.Remove(rss20FeedItem);
                    }
                    catch (Exception e)
                    {
                        sbRet.Append($"\nException: {e.Message}:\n{feed.Title}");
                    }
                }
            }

            sbRet.Append($"\nProcessed {procCount}/{itemCount} items. ({itemCount - procCount} previously processed or do not include a publish date)");
            return sbRet.ToString();
        }

        private static async Task<string> ProcessAtomFeed(AtomFeed feed, RssFeed rssFeed)
        {
            var sbRet = new StringBuilder();
            sbRet.Append($"\nFeed Type: Atom\nFeed Title: {feed.Title}\nGenerator: {feed.Generator}");

            var itemCount = feed.Items.Count;
            var procCount = 0;
           

            while (feed.Items.Any())
            {
                var atomFeedItem = (AtomFeedItem)feed.Items.Last();

                if (atomFeedItem.PublishedDate <= rssFeed.LastProcessedItem || atomFeedItem.PublishedDate == null)
                {
                    feed.Items.Remove(atomFeedItem);
                }
                else
                {
                    var converter = new Converter();

                    var message = new MattermostMessage
                    {
                        Channel = rssFeed.BotChannelOverride == "" ? null : rssFeed.BotChannelOverride,
                        Username = rssFeed.BotNameOverride == "" ? null : rssFeed.BotNameOverride,
                        IconUrl = rssFeed.BotImageOverride == "" ? null : new Uri(rssFeed.BotImageOverride),
                        Attachments = new List<MattermostAttachment>
                        {
                            new MattermostAttachment
                            {
                                Pretext = rssFeed.FeedPretext,
                                Title = atomFeedItem.Title ?? "",
                                TitleLink = atomFeedItem.Link == null ? null : new Uri(atomFeedItem.Link),
                                Text = converter.Convert(rssFeed.IncludeContent
                                    ? atomFeedItem.Content ?? atomFeedItem.Summary ?? ""
                                    : atomFeedItem.Summary ?? ""),
                                AuthorName = atomFeedItem.Author.Name ?? "",
                                AuthorLink = atomFeedItem.Author.Uri == null ? null : new Uri(atomFeedItem.Author.Uri)
                            }
                        }
                    };
                    try
                    {
                        await Program.PostToMattermost(message);
                        rssFeed.LastProcessedItem = atomFeedItem.PublishedDate;
                        procCount++;
                        feed.Items.Remove(atomFeedItem);
                    }
                    catch (Exception e)
                    {
                        sbRet.Append($"\nException: {e.Message}:\n{feed.Title}");
                    }
                }
            }

            sbRet.Append($"\nProcessed {procCount}/{itemCount} items. ({itemCount - procCount} previously processed or do not include a publish date)");
            return sbRet.ToString();
        }
    }
}

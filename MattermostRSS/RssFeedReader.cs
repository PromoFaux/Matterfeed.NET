using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using Matterhook.NET.MatterhookClient;
using ReverseMarkdown;

namespace MattermostRSS
{
    internal class RssFeedReader
    {
        internal static async Task PeriodicRssAsync(TimeSpan interval, List<RssFeed> rssFeeds)
        {
            while (true)
            {
                foreach (var feed in rssFeeds)
                {
                    var stuffToLog = $"\n{DateTime.Now}\nFetching RSS URL: {feed.Url}";

                    Feed rssFeed;
                    try
                    {
                        rssFeed = await FeedReader.ReadAsync(feed.Url);
                    }
                    catch (Exception e)
                    {
                        stuffToLog += $"\n Unable to get feed. Exception: {e.Message}";
                        Console.WriteLine(stuffToLog);
                        continue;
                    }

                    switch (rssFeed.Type)
                    {
                        case FeedType.Atom:
                            stuffToLog += await ProcessAtomFeed((AtomFeed)rssFeed.SpecificFeed, feed);
                            break;
                        case FeedType.Rss:
                            Console.WriteLine("FeedType: RSS");
                            break;
                        case FeedType.Rss_2_0:
                            stuffToLog += await ProcessRss20Feed((Rss20Feed)rssFeed.SpecificFeed, feed);
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
                        case FeedType.Unknown:
                            Console.WriteLine("FeedType: Unknown");
                            break;
                        default:
                            Console.WriteLine("FeedType: Unknown");
                            break;
                    }

                    Console.WriteLine(stuffToLog);
                }
                Program.SaveConfigSection(rssFeeds);
                await Task.Delay(interval);
            }

        }

        private static async Task<string> ProcessRss20Feed(Rss20Feed feed, RssFeed rssFeed)
        {
            var retVal = $"\nFeed Type: Rss 2.0\nFeed Title: {feed.Title}\nGenerator: {feed.Generator}";

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
                        retVal += $"\nException: {e.Message}:\n{feed.Title}";
                    }
                }
            }

            retVal +=
                $"\nProcessed {procCount}/{itemCount} items. ({itemCount - procCount} previously processed or do not include a publish date)";
            return retVal;
        }

        private static async Task<string> ProcessAtomFeed(AtomFeed feed, RssFeed rssFeed)
        {
            var retval = $"\nFeed Type: Atom\nFeed Title: {feed.Title}\nGenerator: {feed.Generator}";

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
                        retval += $"\nException: {e.Message}:\n{feed.Title}";
                    }
                }
            }

            retval +=
                $"\nProcessed {procCount}/{itemCount} items. ({itemCount - procCount} previously processed or do not include a publish date)";
            return retval;
        }
    }
}

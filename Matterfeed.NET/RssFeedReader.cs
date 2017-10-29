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
                foreach (var rssFeedConfig in rssFeeds)
                {
                    var sbOut = new StringBuilder();
                    sbOut.Append($"\n{DateTime.Now}\nFetching RSS URL: {rssFeedConfig.Url}");

                    Feed newFeed;

                    //Get Feed from URL
                    try
                    {
                        newFeed = await FeedReader.ReadAsync(rssFeedConfig.Url);
                    }
                    catch (Exception e)
                    {
                        sbOut.Append($"\n Unable to get newFeed. Exception: {e.Message}");
                        Console.WriteLine(sbOut.ToString());
                        continue;
                    }

                    sbOut.Append($"\nFeed Title: {newFeed.Title}");

                    var itemCount = newFeed.Items.Count;
                    var procCount = 0;

                    //Fallback mode is for feeds that don't properly use published date.
                    //If we're in fallback mode, then check for previously saved copy of feed, load it into memory and then overwrite it with newest copy
                    Feed oldFeed = null;
                    if (rssFeedConfig.FallbackMode)
                    {
                        var filename = $"/config/{newFeed.Title.Replace(" ", "_")}.xml";

                        sbOut.Append($"\nFallback Mode Enabled, checking for {filename}");
                        if (System.IO.File.Exists(filename))
                        {
                            sbOut.Append($"\nLoading old feed from {filename}");
                            oldFeed = FeedReader.ReadFromFile(filename);
                        }

                        sbOut.Append($"\nWriting new feed to {filename}");
                        System.IO.File.WriteAllText(filename, newFeed.OriginalDocument);
                    }

                    //Loop through new items just downloaded
                    foreach (var newFeedItem in newFeed.Items.OrderBy(x=>x.PublishingDate))
                    {
                        //if we're in fallback mode and we have an old feed, does the new item exist in that?
                        IEnumerable<BaseFeedItem> dupeItems = null;
                        if (oldFeed != null)
                        {
                            dupeItems = oldFeed.SpecificFeed.Items.Where(x => x.Title == newFeedItem.Title);
                        }
                        
                        if (rssFeedConfig.FallbackMode && (dupeItems!=null && dupeItems.Any())) continue; //Item exists in old file
                        if (!rssFeedConfig.FallbackMode && (newFeedItem.PublishingDate <= rssFeedConfig.LastProcessedItem || newFeedItem.PublishingDate == null)) continue; // Item was previously processed or has no published date

                        var content = "";
                        MattermostMessage mm = null;
                        switch (newFeed.Type)
                        {
                            case FeedType.Atom:
                                var tmpAf = (AtomFeedItem)newFeedItem.SpecificItem;
                                content = !rssFeedConfig.IncludeContent || tmpAf.Content == null
                                        ? (tmpAf.Summary != null
                                        ? (tmpAf.Summary.Length < 500 ? tmpAf.Summary : "")
                                        : "")
                                    : tmpAf.Content;
                                //content = rssFeedConfig.IncludeContent ? tmpAf.Content ?? tmpAf.Summary ?? "": tmpAf.Summary ?? "";
                                
                                var link = tmpAf.Links.FirstOrDefault(x => x.Relation == "alternate");
                                var url = link == null ? tmpAf.Link : link.Href;

                                mm = MattermostMessage(rssFeedConfig, tmpAf.Title, url, content,
                                    tmpAf.Author.Name);
                                mm.Text = tmpAf.Title;
                                break;
                            case FeedType.Rss_0_91:
                                break;
                            case FeedType.Rss_0_92:
                                break;
                            case FeedType.Rss_1_0:
                                break;
                            case FeedType.Rss_2_0:
                                var tmpR2 = (Rss20FeedItem)newFeedItem.SpecificItem;
                                content = !rssFeedConfig.IncludeContent || tmpR2.Content == null
                                    ? (tmpR2.Description != null
                                        ? (tmpR2.Description.Length < 500 ? tmpR2.Description : "")
                                        : "")
                                    : tmpR2.Content;
                                mm = MattermostMessage(rssFeedConfig, tmpR2.Title, tmpR2.Link, content,
                                    tmpR2.Author);
                                break;
                            case FeedType.Rss:
                                break;
                            case FeedType.Unknown:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        if (mm == null) continue;

                        await Program.PostToMattermost(mm);
                        if (!rssFeedConfig.FallbackMode)
                        {
                            rssFeedConfig.LastProcessedItem = newFeedItem.PublishingDate;
                        }
                        procCount++;
                    }

                    var tmp = rssFeedConfig.FallbackMode
                        ? "exist in file from previous run"
                        : "previously processed or have no publish date";
                    sbOut.Append($"\nProcessed {procCount}/{itemCount} items. ({itemCount - procCount} {tmp})");

                    Console.WriteLine(sbOut.ToString());
                }
                //update config file with lastprocesed date
                Program.SaveConfigSection(rssFeeds);
                await Task.Delay(interval).ConfigureAwait(false);
            }

        }

        private static MattermostMessage MattermostMessage(RssFeed rssFeedConfig, string title, string link, string attText, string author)
        {

            var converter = new Converter();

            var message = new MattermostMessage
            {
                Channel = rssFeedConfig.BotChannelOverride == "" ? null : rssFeedConfig.BotChannelOverride,
                Username = rssFeedConfig.BotNameOverride == "" ? null : rssFeedConfig.BotNameOverride,
                IconUrl = rssFeedConfig.BotImageOverride == "" ? null : new Uri(rssFeedConfig.BotImageOverride),
                Attachments = new List<MattermostAttachment>
                {
                    new MattermostAttachment
                    {
                        Pretext = rssFeedConfig.FeedPretext,
                        Title = title ?? "",
                        TitleLink = link  == null ? null : new Uri(link),
                        Text = converter.Convert(attText),
                        AuthorName = author
                    }
                }
            };
            return message;
        }
      
    }
}

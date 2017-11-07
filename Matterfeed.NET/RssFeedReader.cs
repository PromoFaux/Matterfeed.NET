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
        internal static async Task PeriodicRssAsync(RssFeedConfig rssFeedConfig)
        {
            while (true)
            {
                var logit = false;
                var sbOut = new StringBuilder();

                foreach (var rssFeed in rssFeedConfig.RssFeeds)
                {
                    sbOut.Append($"\n{DateTime.Now}\nFetching RSS URL: {rssFeed.Url}");

                    Feed newFeed;

                    //Get Feed from URL
                    try
                    {
                        newFeed = await FeedReader.ReadAsync(rssFeed.Url);
                    }
                    catch (Exception e)
                    {
                        sbOut.Append($"\n ERROR: Unable to get Feed. Exception: {e.Message}");
                        Console.WriteLine(sbOut.ToString());
                        //unable to get this feed, move onto next feed.
                        continue;
                    }

                    sbOut.Append($"\nFeed Title: {newFeed.Title}");

                    switch (newFeed.Type)
                    {
                        case FeedType.Atom:
                            break;
                        case FeedType.Rss_2_0:
                            break;
                        default:
                            sbOut.Append($"\nERROR: Unhandled Feed type: {newFeed.Type}");
                            Console.WriteLine(sbOut.ToString());
                            continue;
                    }

                    //Fallback mode is for feeds that don't properly use published date.
                    //If we're in fallback mode, then check for previously saved copy of feed, load it into memory and then overwrite it with newest copy
                    Feed oldFeed = null;
                    if (rssFeed.FallbackMode)
                    {
                        try
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
                        catch (Exception e)
                        {
                            sbOut.Append($"\nERROR: Fallback mode failed ({e.Message}");
                            Console.WriteLine(sbOut.ToString());
                            continue;
                        }
                        
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
                        
                        if (rssFeed.FallbackMode && (dupeItems!=null && dupeItems.Any())) continue; //Item exists in old file
                        if (!rssFeed.FallbackMode && (newFeedItem.PublishingDate <= rssFeed.LastProcessedItem || newFeedItem.PublishingDate == null)) continue; // Item was previously processed or has no published date

                        string content;
                        MattermostMessage mm = null;

                        //Get contents of Mattermost message depending on feed type. 
                        if (newFeed.Type == FeedType.Atom)
                        {
                            //Process Atom Feed Item
                            var tmpAf = (AtomFeedItem) newFeedItem.SpecificItem;
                            content = !rssFeed.IncludeContent || tmpAf.Content == null
                                ? (tmpAf.Summary != null
                                    ? (tmpAf.Summary.Length < 500 ? tmpAf.Summary : "")
                                    : "")
                                : tmpAf.Content;

                            var link = tmpAf.Links.FirstOrDefault(x => x.Relation == "alternate");
                            var url = link == null ? tmpAf.Link : link.Href;

                            mm = MattermostMessage(rssFeed, tmpAf.Title, url, content,
                                tmpAf.Author.Name);
                            mm.Text = tmpAf.Title;
                        }
                        else if (newFeed.Type == FeedType.Rss_2_0)
                        {
                            //Process RSS 2.0 Item
                            var tmpR2 = (Rss20FeedItem) newFeedItem.SpecificItem;
                            content = !rssFeed.IncludeContent || tmpR2.Content == null
                                ? (tmpR2.Description != null
                                    ? (tmpR2.Description.Length < 500 ? tmpR2.Description : "")
                                    : "")
                                : tmpR2.Content;
                            mm = MattermostMessage(rssFeed, tmpR2.Title, tmpR2.Link, content,
                                tmpR2.Author);
                        }

                        if (mm == null) continue; //Shouldn't be, but let's try and catch it anyway

                        try
                        {
                            await Program.PostToMattermost(mm);
                            if (!rssFeed.FallbackMode)
                            {
                                rssFeed.LastProcessedItem = newFeedItem.PublishingDate;
                            }
                        }
                        catch (Exception e)
                        {
                            sbOut.Append($"Exception: {e.Message}");
                            logit = true;
                            break;
                        }
                        
                    }

                    if (!logit) continue;
                    Console.WriteLine(sbOut.ToString());
                    break; //abandon rss processing until the next processing time
                }

                //update config file with lastprocesed date
                Program.SaveConfigSection(rssFeedConfig);
                await Task.Delay(TimeSpan.FromMilliseconds(rssFeedConfig.Interval)).ConfigureAwait(false);
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

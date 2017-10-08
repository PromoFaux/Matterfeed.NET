using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Matterhook.NET.MatterhookClient;
using Newtonsoft.Json;

namespace Matterfeed.NET
{
    internal class RedditJsonFeedReader
    {
        public static async Task PeriodicRedditAsync(TimeSpan interval, List<RedditJsonFeed> redditFeeds)
        {
            while (true)
            {
                foreach (var feed in redditFeeds)
                {
                    using (var wc = new WebClient())
                    {
                        var stuffToLog = $"\n{DateTime.Now}\nFetching Reddit URL: {feed.Url}";

                        string json;
                        try
                        {
                            json = wc.DownloadString(feed.Url);
                        }
                        catch (Exception e)
                        {
                            stuffToLog += $"\nUnable to get feed, exception: {e.Message}";
                            Console.WriteLine(stuffToLog);
                            return;
                        }

                        //only get items we have not already processed


                        var items = JsonConvert.DeserializeObject<RedditJson>(json).RedditJsonData.RedditJsonChildren
                            .Where(y => y.Data.Created > feed.LastProcessedItem).OrderBy(x => x.Data.Created);

                        var itemCount = items.Count();
                        var procCount = 0;

                        foreach (var item in items)
                        {
                            var message = new MattermostMessage
                            {
                                Channel = feed.BotChannelOverride == "" ? null : feed.BotChannelOverride,
                                Username = feed.BotNameOverride == "" ? null : feed.BotNameOverride,
                                IconUrl = feed.BotImageOverride == "" ? null : new Uri(feed.BotImageOverride)
                            };

                            switch (item.Kind)
                            {
                                case "t3":
                                    string content;
                                    switch (item.Data.PostHint)
                                    {
                                        case "link":
                                            content = $"Linked Content: {item.Data.Url}";
                                            break;
                                        default:
                                            content = item.Data.Selftext;
                                            break;
                                    }

                                    message.Attachments = new List<MattermostAttachment>
                                    {
                                        new MattermostAttachment
                                        {
                                            AuthorName = $"/u/{item.Data.Author}",
                                            AuthorLink = new Uri($"https://reddit.com/u/{item.Data.Author}"),
                                            Title = item.Data.Title,
                                            TitleLink = new Uri($"https://reddit.com{item.Data.Permalink}"),
                                            Text = content,
                                            Pretext = feed.FeedPretext
                                        }
                                    };
                                    message.Text =
                                        $"#{Regex.Replace(item.Data.Title.Replace(" ", "-"), "[^0-9a-zA-Z-]+", "")}";

                                    break;
                                case "t4":

                                    message.Attachments = new List<MattermostAttachment>
                                    {
                                        new MattermostAttachment
                                        {
                                            AuthorName = $"/u/{item.Data.Author}",
                                            AuthorLink = new Uri($"https://reddit.com/u/{item.Data.Author}"),
                                            Title = item.Data.Subject,
                                            TitleLink = new Uri($"https://reddit.com{item.Data.Permalink}"),
                                            Text =
                                                item.Data.Body.Replace("](/r/",
                                                    "](https://reddit.com/r/"), //expand /r/ markdown links
                                            Pretext = feed.FeedPretext
                                        }
                                    };
                                    break;
                            }


                            try
                            {
                                //Task.WaitAll(Program.PostToMattermost(message));
                                await Program.PostToMattermost(message);
                                feed.LastProcessedItem = item.Data.Created;
                                procCount++;
                            }
                            catch (Exception e)
                            {
                                stuffToLog += $"\nException: {e.Message}";
                            }
                        }

                        stuffToLog += $"\nProcessed {procCount}/{itemCount} items.";
                        Console.WriteLine(stuffToLog);
                    }
                }
                Program.SaveConfigSection(redditFeeds);
                await Task.Delay(interval);
            }
        }
    }
}
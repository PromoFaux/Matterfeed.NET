using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Matterhook.NET.MatterhookClient;
using Newtonsoft.Json;

namespace Matterfeed.NET
{
    internal static class RedditJsonFeedReader
    {
        public static async Task PeriodicRedditAsync(RedditFeedConfig reddifFeedConfig)
        {
            while (true)
            {
                foreach (var feed in reddifFeedConfig.RedditJsonFeeds)
                {
                    using (var wc = new WebClient())
                    {
                        var logit = false;
                        var sbOut = new StringBuilder();
                        sbOut.Append($"\n{DateTime.Now}\nFetching Reddit URL: {feed.Url}");
                        
                        string json;
                        try
                        {
                            json = wc.DownloadString(feed.Url);
                        }
                        catch (Exception e)
                        {
                            sbOut.Append($"\nERROR: Unable to get feed, exception: {e.Message}");
                            Console.WriteLine(sbOut.ToString());
                            continue;
                        }

                        //only get items we have not already processed


                        var items = JsonConvert.DeserializeObject<RedditJson>(json).RedditJsonData.RedditJsonChildren
                            .Where(y => y.Data.Created > feed.LastProcessedItem).OrderBy(x => x.Data.Created);

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
                                    var content = item.Data.PostHint == "link" ? $"Linked Content: {item.Data.Url}" : item.Data.Selftext;

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
                                case "t1":
                                case "t4":

                                    var title = item.Data.LinkTitle != null ? $"{item.Data.Subject} - {item.Data.LinkTitle}":item.Data.Subject;

                                    message.Attachments = new List<MattermostAttachment>
                                    {
                                        new MattermostAttachment
                                        {
                                            AuthorName = $"/u/{item.Data.Author}",
                                            AuthorLink = new Uri($"https://reddit.com/u/{item.Data.Author}"),
                                            Title = title,
                                            TitleLink = item.Data.Context != "" ?  new Uri($"https://reddit.com{item.Data.Context}") : null,
                                            Text =
                                                item.Data.Body.Replace("](/r/",
                                                    "](https://reddit.com/r/"), //expand /r/ markdown links, does not correctly parse promoted links
                                            Pretext = feed.FeedPretext
                                        }
                                    };
                                    break;
                            }


                            try
                            {
                                await Program.PostToMattermost(message);
                                feed.LastProcessedItem = item.Data.Created;
                            }
                            catch (Exception e)
                            {
                                sbOut.Append($"\nException: {e.Message}");
                                logit = true;
                                break;

                            }
                        }

                        if(!logit) continue;
                        Console.WriteLine(sbOut.ToString());
                        break;
                    }
                }
                Program.SaveConfigSection(reddifFeedConfig);
                await Task.Delay(TimeSpan.FromMilliseconds(reddifFeedConfig.Interval)).ConfigureAwait(false);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using Newtonsoft.Json;
using Matterhook.NET.MatterhookClient;
using Newtonsoft.Json.Linq;
using Converter = ReverseMarkdown.Converter;

namespace MattermostRSS
{
    internal class Program
    {
        private const string ConfigPath = "/config/secrets.json";

        public static Config Config = new Config();

        private static void Main(string[] args)
        {
            Console.WriteLine($"{DateTime.Now} - Application Started.");

            LoadConfig();

            while (true)
            {
                //Loop forever. There is probably a more graceful way to do this. 
                try // lazy try catch, let's see what errors get thrown...
                {
                    if (Config.RssFeeds != null)
                    {
                        foreach (var feed in Config.RssFeeds)
                        {
                            Task.WaitAll(ProcessRss(feed));
                        }
                    }

                    if (Config.RedditJsonFeeds != null)
                    {
                        foreach (var feed in Config.RedditJsonFeeds)
                        {
                            Task.WaitAll(ProcessReddit(feed));
                        }
                    }

                    GC.Collect();

                    Thread.Sleep(Config.BotCheckIntervalMs);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }


        private static void LoadConfig()
        {
            if (File.Exists(ConfigPath))
                using (var file = File.OpenText(ConfigPath))
                {
                    var serializer = new JsonSerializer();
                    Config = (Config)serializer.Deserialize(file, typeof(Config));
                }
            else
            {
                Console.WriteLine("No secrets.json found! I Have no idea what to do...");
                Environment.Exit(1);
            }
        }

        #region RSS

        private static async Task ProcessRss(RssFeed rssFeed)
        {
            Console.WriteLine("");
            var feed = await GetRssFeed(rssFeed.Url);
            Console.WriteLine($"Feed Title: {feed.Title}");


            switch (feed.Type)
            {
                case FeedType.Atom:
                    Console.WriteLine("FeedType: Atom");
                    await ProcessAtomFeed((AtomFeed)feed.SpecificFeed, rssFeed);
                    break;
                case FeedType.Rss:
                    Console.WriteLine("FeedType: RSS");
                    break;
                case FeedType.Rss_2_0:
                    Console.WriteLine("FeedType: RSS 2.0");
                    await ProcessRss20Feed((Rss20Feed)feed.SpecificFeed, rssFeed);
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
        }

        private static async Task ProcessRss20Feed(Rss20Feed feed, RssFeed rssFeed)
        {
            Console.WriteLine($"Generator: {feed.Generator}");

            while (feed.Items.Any())
            {
                var rss20FeedItem = (Rss20FeedItem)feed.Items.Last();


                if (rss20FeedItem.PublishingDate <= rssFeed.LastProcessedItem)
                {
                    feed.Items.Remove(rss20FeedItem);
                }
                else
                {
                    Console.WriteLine($"Posting: {rss20FeedItem.Title}");
                    var converter = new Converter();

                    var message = new MattermostMessage
                    {
                        Channel = rssFeed.BotChannelOverride == ""
                            ? Config.BotChannelDefault
                            : rssFeed.BotChannelOverride,
                        Username = rssFeed.BotNameOverride == ""
                            ? Config.BotNameDefault
                            : rssFeed.BotNameOverride,
                        IconUrl = rssFeed.BotImageOverride == ""
                            ? new Uri(Config.BotImageDefault)
                            : new Uri(rssFeed.BotImageOverride),
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

                    var response = await PostToMattermost(message);

                    if (response == null || response.StatusCode != HttpStatusCode.OK)
                    {
                        Console.WriteLine(response != null
                            ? $"Unable to post to Mattermost {response.StatusCode}"
                            : "Unable to post to Mattermost");
                    }
                    else
                    {
                        Console.WriteLine("Succesfully posted to Mattermost");
                        rssFeed.LastProcessedItem = rss20FeedItem.PublishingDate;
                        Config.Save(ConfigPath);
                    }

                    feed.Items.Remove(rss20FeedItem);
                }
            }
        }

        private static async Task ProcessAtomFeed(AtomFeed feed, RssFeed rssFeed)
        {
            Console.WriteLine("Generator: " + feed.Generator);
            Console.WriteLine("Logo: " + feed.Logo);
            //feed

            while (feed.Items.Any())
            {
                var atomFeedItem = (AtomFeedItem)feed.Items.Last();

                if (atomFeedItem.PublishedDate <= rssFeed.LastProcessedItem)
                {
                    feed.Items.Remove(atomFeedItem);
                }
                else
                {
                    Console.WriteLine($"Posting: {atomFeedItem.Title}");
                    var converter = new Converter();

                    var message = new MattermostMessage
                    {
                        Channel = rssFeed.BotChannelOverride == ""
                            ? Config.BotChannelDefault
                            : rssFeed.BotChannelOverride,
                        Username = rssFeed.BotNameOverride == ""
                            ? Config.BotNameDefault
                            : rssFeed.BotNameOverride,
                        IconUrl = rssFeed.BotImageOverride == ""
                            ? new Uri(Config.BotImageDefault)
                            : new Uri(rssFeed.BotImageOverride),
                        Attachments = new List<MattermostAttachment>
                        {
                            new MattermostAttachment
                            {
                                Pretext = rssFeed.FeedPretext,
                                Title = atomFeedItem.Title ?? "",
                                TitleLink = atomFeedItem.Link == null ? null : new Uri(atomFeedItem.Link),
                                Text = converter.Convert(rssFeed.IncludeContent
                                    ? atomFeedItem.Content ?? atomFeedItem.Summary ?? "No Content or Description"
                                    : atomFeedItem.Summary ?? ""),
                                AuthorName = atomFeedItem.Author.Name ?? "",
                                AuthorLink = atomFeedItem.Author.Uri == null ? null : new Uri(atomFeedItem.Author.Uri)
                            }
                        }
                    };

                    var response = await PostToMattermost(message);

                    if (response == null || response.StatusCode != HttpStatusCode.OK)
                    {
                        Console.WriteLine(response != null
                            ? $"Unable to post to Mattermost {response.StatusCode}"
                            : "Unable to post to Mattermost");
                    }
                    else
                    {
                        Console.WriteLine("Succesfully posted to Mattermost");
                        rssFeed.LastProcessedItem = atomFeedItem.PublishedDate;
                        Config.Save(ConfigPath);
                    }

                    feed.Items.Remove(atomFeedItem);
                }
            }
        }


        public static async Task<Feed> GetRssFeed(string url)
        {
            try
            {
                return await FeedReader.ReadAsync(url);
            }
            catch (Exception e)
            {
                //Problem getting the feed.
                Console.WriteLine($"Problem retrieving feed\n Exception Message: {e.Message}");
                return null;
            }
        }

        #endregion

        #region RedditJSONFeeds

        private static async Task ProcessReddit(RedditJsonFeed feed)
        {
            using (var wc = new WebClient())
            {
                var json = wc.DownloadString(feed.Url);
                var items = JsonConvert.DeserializeObject<RedditJson>(json).RedditJsonData.RedditJsonChildren
                    .Where(y => y.Data.Created > feed.LastProcessedItem).OrderBy(x => x.Data.Created);

                if (!items.Any()) return;

                foreach (var item in items)
                {
                    var message = new MattermostMessage
                    {
                        Channel = feed.BotChannelOverride == ""
                            ? Config.BotChannelDefault
                            : feed.BotChannelOverride,
                        Username = feed.BotNameOverride == ""
                            ? Config.BotNameDefault
                            : feed.BotNameOverride,
                        IconUrl = feed.BotImageOverride == ""
                            ? new Uri(Config.BotImageDefault)
                            : new Uri(feed.BotImageOverride)
                        
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
                            message.Text = $"#{Regex.Replace(item.Data.Title.Replace(" ", "-"), "[^0-9a-zA-Z-]+", "")}";

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
                                    Text = item.Data.Body.Replace("](/r/","](https://reddit.com/r/"), //expand /r/ markdown links
                                    Pretext = feed.FeedPretext
                                }
                            };

                            //message.Attachments = new List<MattermostAttachment> { GetInboxAttachment(item.Data) };
                            break;
                    }

                    var response = await PostToMattermost(message);

                    if (response == null || response.StatusCode != HttpStatusCode.OK)
                    {
                        Console.WriteLine(response != null
                            ? $"Unable to post to Mattermost {response.StatusCode}"
                            : "Unable to post to Mattermost");
                    }
                    else
                    {
                        Console.WriteLine("Succesfully posted to Mattermost");
                        feed.LastProcessedItem = item.Data.Created;
                        Config.Save(ConfigPath);
                    }

                }

                Console.WriteLine("hello");
            }
        }




        #endregion

        public static async Task<HttpResponseMessage> PostToMattermost(MattermostMessage message)
        {
            var mc = new MatterhookClient(Config.MattermostWebhookUrl);
            var response = await mc.PostAsync(message);
            return response;
        }
    }
}
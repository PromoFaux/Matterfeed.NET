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
using Tweetinvi;
using Tweetinvi.Json;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using Converter = ReverseMarkdown.Converter;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

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
                    //if (Config.RssFeeds != null)
                    //{
                    //    foreach (var feed in Config.RssFeeds)
                    //    {
                    //        Task.WaitAll(ProcessRss(feed));
                    //    }
                    //}

                    //if (Config.RedditJsonFeeds != null)
                    //{
                    //    foreach (var feed in Config.RedditJsonFeeds)
                    //    {
                    //        Task.WaitAll(ProcessReddit(feed));
                    //    }
                    //}

                    if (Config.TwitterFeed != null)
                    {
                        ProcessTwitter();
                    }


                    GC.Collect();

                    Thread.Sleep(Config.BotCheckIntervalMs);
                }
                catch (Exception e)
                {
                    //this is terrible error handling.
                    Console.WriteLine("-------------------------------------------------------------");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("-------------------------------------------------------------");
                }
            }
        }


        private static void LoadConfig()
        {
            if (File.Exists(ConfigPath))
                using (var file = File.OpenText(ConfigPath))
                {
                    var serializer = new JsonSerializer();
                    Config = (Config) serializer.Deserialize(file, typeof(Config));
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
            var stuffToLog = $"\n{DateTime.Now}\nFetching RSS URL: {rssFeed.Url}";

            Feed feed;

            try
            {
                feed = await FeedReader.ReadAsync(rssFeed.Url);
            }
            catch (Exception e)
            {
                stuffToLog += $"\n Unable to get feed. Exception: {e.Message}";
                Console.WriteLine(stuffToLog);
                return;
            }


            switch (feed.Type)
            {
                case FeedType.Atom:
                    stuffToLog += await ProcessAtomFeed((AtomFeed) feed.SpecificFeed, rssFeed);
                    break;
                case FeedType.Rss:
                    Console.WriteLine("FeedType: RSS");
                    break;
                case FeedType.Rss_2_0:
                    stuffToLog += await ProcessRss20Feed((Rss20Feed) feed.SpecificFeed, rssFeed);
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

        private static async Task<string> ProcessRss20Feed(Rss20Feed feed, RssFeed rssFeed)
        {
            var retVal = $"\nFeed Type: Rss 2.0\nFeed Title: {feed.Title}\nGenerator: {feed.Generator}";

            var itemCount = feed.Items.Count;
            var procCount = 0;
            var failedMmPostCount = 0;

            while (feed.Items.Any())
            {
                var rss20FeedItem = (Rss20FeedItem) feed.Items.Last();

                if (rss20FeedItem.PublishingDate <= rssFeed.LastProcessedItem || rss20FeedItem.PublishingDate == null)
                {
                    feed.Items.Remove(rss20FeedItem);
                }
                else
                {
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
                        //Try again up to three times, if it fails, give up.
                        if (failedMmPostCount == 3)
                        {
                            retVal += response != null
                                ? $"\nUnable to post to Mattermost, abandoning feed.{response.StatusCode}"
                                : $"\nUnable to post to Mattermost, abandoning feed.";
                            return retVal;
                        }

                        failedMmPostCount++;
                    }
                    else
                    {
                        //Console.WriteLine("Succesfully posted to Mattermost");
                        rssFeed.LastProcessedItem = rss20FeedItem.PublishingDate;
                        Config.Save(ConfigPath);
                        procCount++;
                        feed.Items.Remove(rss20FeedItem);
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
            var failedMmPostCount = 0;
            //feed

            while (feed.Items.Any())
            {
                var atomFeedItem = (AtomFeedItem) feed.Items.Last();

                if (atomFeedItem.PublishedDate <= rssFeed.LastProcessedItem || atomFeedItem.PublishedDate == null)
                {
                    feed.Items.Remove(atomFeedItem);
                }
                else
                {
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
                                    ? atomFeedItem.Content ?? atomFeedItem.Summary ?? ""
                                    : atomFeedItem.Summary ?? ""),
                                AuthorName = atomFeedItem.Author.Name ?? "",
                                AuthorLink = atomFeedItem.Author.Uri == null ? null : new Uri(atomFeedItem.Author.Uri)
                            }
                        }
                    };
                    var response = await PostToMattermost(message);

                    if (response == null || response.StatusCode != HttpStatusCode.OK)
                    {
                        //Try again up to three times, if it fails, give up.
                        if (failedMmPostCount == 3)
                        {
                            retval += response != null
                                ? $"\nUnable to post to Mattermost, abandoning feed.{response.StatusCode}"
                                : "\nUnable to post to Mattermost, abandoning feed.";
                            return retval;
                        }

                        failedMmPostCount++;
                    }
                    else
                    {
                        //Console.WriteLine("Succesfully posted to Mattermost");
                        rssFeed.LastProcessedItem = atomFeedItem.PublishedDate;
                        Config.Save(ConfigPath);
                        procCount++;
                        feed.Items.Remove(atomFeedItem);
                    }
                }
            }

            retval +=
                $"\nProcessed {procCount}/{itemCount} items. ({itemCount - procCount} previously processed or do not include a publish date)";
            return retval;
        }

        #endregion

        #region RedditJSONFeeds

        private static async Task ProcessReddit(RedditJsonFeed feed)
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
                var failedMmPostCount = 0;

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
                                    Text =
                                        item.Data.Body.Replace("](/r/",
                                            "](https://reddit.com/r/"), //expand /r/ markdown links
                                    Pretext = feed.FeedPretext
                                }
                            };
                            break;
                    }

                    var response = await PostToMattermost(message);

                    if (response == null || response.StatusCode != HttpStatusCode.OK)
                    {
                        //Try again up to three times, if it fails, give up.
                        if (failedMmPostCount == 3)
                        {
                            stuffToLog += response != null
                                ? $"\nUnable to post to Mattermost, abandoning feed.{response.StatusCode}"
                                : $"\nUnable to post to Mattermost, abandoning feed.";
                            Console.WriteLine(stuffToLog);
                            return;
                        }

                        failedMmPostCount++;
                    }
                    else
                    {
                        //"Succesfully posted to Mattermost");
                        feed.LastProcessedItem = item.Data.Created;
                        Config.Save(ConfigPath);
                        procCount++;
                    }
                }

                stuffToLog += $"\nProcessed {procCount}/{itemCount} items.";
                Console.WriteLine(stuffToLog);
            }
        }

        #endregion

        #region Twitter

        private static async void ProcessTwitter()
        {
            var retval = $"\n{DateTime.Now}\nTwitter\n";

            Auth.SetUserCredentials(Config.TwitterFeed.ConsumerKey, Config.TwitterFeed.ConsumerSecret,
                Config.TwitterFeed.AccessToken, Config.TwitterFeed.AccessTokenSecret);
            var authenticatedUser = User.GetAuthenticatedUser();
            Console.WriteLine(authenticatedUser);
            var rateLimits = RateLimit.GetCurrentCredentialsRateLimits();
            Console.WriteLine(rateLimits.SearchTweetsLimit);

            if (Config.TwitterFeed.Searches.Any())
            {
                foreach (var s in Config.TwitterFeed.Searches)
                {
                    var searchParameter = new SearchTweetsParameters(s.SearchTerm)
                    {
                        SearchType = SearchResultType.Mixed,
                        MaximumNumberOfResults = 100,
                        SinceId = s.LastProcessedId
                    };

                    var tweets = Search.SearchTweets(searchParameter);

                    foreach (var t in tweets.OrderBy(x => x.Id))
                    {
                        var message = new MattermostMessage
                        {
                            Channel = s.BotChannelOverride == ""
                                ? Config.BotChannelDefault
                                : s.BotChannelOverride,
                            Username = s.BotNameOverride == ""
                                ? Config.BotNameDefault
                                : s.BotNameOverride,
                            IconUrl = s.BotImageOverride == ""
                                ? new Uri(Config.BotImageDefault)
                                : new Uri(s.BotImageOverride),
                            Attachments = new List<MattermostAttachment>
                            {
                                new MattermostAttachment
                                {
                                    Pretext = "tweet",
                                    Title = "New Tweet from Search Result",
                                    TitleLink = new Uri(t.Url??""),
                                    Text = $">{t.FullText??""}",
                                    AuthorName = t.CreatedBy.Name??"",
                                    AuthorLink = t.CreatedBy.Url == null? null:new Uri(t.CreatedBy.Url),
                                    AuthorIcon = new Uri(t.CreatedBy.ProfileImageUrl400x400??"")
                                }
                            }
                        };
                        var response = await PostToMattermost(message);

                        if (response == null || response.StatusCode != HttpStatusCode.OK)
                        {
                            //Try again up to three times, if it fails, give up.
                            retval += response != null
                                ? $"\nUnable to post to Mattermost, abandoning feed.{response.StatusCode}"
                                : "\nUnable to post to Mattermost, abandoning feed.";
                            Console.WriteLine(retval);
                            return;
                        }
                        //Console.WriteLine("Succesfully posted to Mattermost");
                        s.LastProcessedId = t.Id;
                        Config.Save(ConfigPath);
                    }
                }
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
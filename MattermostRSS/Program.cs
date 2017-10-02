using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using CodeHollow.FeedReader.Feeds.Itunes;
using Newtonsoft.Json;
using Matterhook.NET.MatterhookClient;

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
                    foreach (var feed in Config.RssFeeds)
                    {
                        Task.WaitAll(ProcessFeeds(feed));
                        
                    }
                    GC.Collect();

                    //Config.Save(ConfigPath);

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

        private static async Task ProcessFeeds(RssFeed rssFeed)
        {


            switch (rssFeed.FeedType)
            {
                case "RSS":
                    break;
                case "JSON":
                    break;
                case null:
                    Console.WriteLine("Feed Type not set");
                    break;
                default :
                    Console.WriteLine("Feed Type not set");
                    break;
            }
            //Below method is obsolete. TODO: Update to use ReadAsync (quickfix for now)
            Console.WriteLine("");
            var feed = await FeedReader.ReadAsync(rssFeed.Url);
            Console.WriteLine("Feed Title: " + feed.Title);
            //var test1 = new List<AtomFeedItem>();

            switch (feed.Type)
            {
                case FeedType.Atom:
                    Console.WriteLine("FeedType: Atom");
                    ProcessAtomFeed((AtomFeed)feed.SpecificFeed, rssFeed);
                    break;
                case FeedType.Rss:
                    Console.WriteLine("FeedType: RSS");
                    
                    ProcessRssFeed(feed.SpecificFeed);
                    break;
                case FeedType.Rss_2_0:
                    Console.WriteLine("FeedType: RSS 2.0");
                    ProcessRss20Feed((Rss20Feed)feed.SpecificFeed);
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
                    throw new ArgumentOutOfRangeException();
            }



            if (feed == null)
            {
                Console.WriteLine($"{DateTime.Now}RSS feed returned null");
                return;
            }

            //Using Feedreader instead of SyndicationToolbox allows us to ignore any feed items with a null Publish date.
            


            //if (!results.Any()) return;

            //var rssItems = new List<RssToMattermostMessage>();

            //////TODO: There is probably a much better way of doing this
            //switch (rssFeed.FeedType)
            //{
            //    case "RedditPost":
            //        rssItems.AddRange(results.Select(fa => new RedditPost(fa, rssFeed.FeedPretext)));
            //        break;
            //    case "RedditInbox":
            //        rssItems.AddRange(results.Select(fa => new RedditInbox(fa, rssFeed.FeedPretext)));
            //        break;
            //    default://Use Generic Feed
            //        rssItems.AddRange(results.Select(fa => new Generic(fa, rssFeed.FeedPretext, rssFeed.IncludeContent)));
            //        break;
            //}

//            foreach (var item in rssItems)
//            {


//                item.Channel = rssFeed.BotChannelOverride == ""
//                    ? Config.BotChannelDefault
//                    : rssFeed.BotChannelOverride;

//                item.Username = rssFeed.BotNameOverride == ""
//                    ? Config.BotNameDefault
//                    : rssFeed.BotNameOverride;

//                item.IconUrl = rssFeed.BotImageOverride == ""
//                    ? new Uri(Config.BotImageDefault)
//                    : new Uri(rssFeed.BotImageOverride);
               

//                PostToMattermost(item);
//                rssFeed.LastProcessedItem = item.FeedItem.PublishingDate;
//#if Release
//                                Config.Save(ConfigPath);
//#endif
//            }




        }

        private static void ProcessRssFeed(BaseFeed feed)
        {
           // var results = feed.Items.Where(x => x.)
            //throw new NotImplementedException();
        }

        private static void ProcessRss20Feed(Rss20Feed feed)
        {
            Console.WriteLine("Generator: " + feed.Generator);
            Console.WriteLine("Logo: " + feed.Image);
            
            // throw new NotImplementedException();
        }

        private static void ProcessAtomFeed(AtomFeed feed, RssFeed rssFeed)
        {
            Console.WriteLine("Generator: " + feed.Generator);
            Console.WriteLine("Logo: " + feed.Logo);
            //feed

            while (feed.Items.Any())
            {
                var atomFeedItem = (AtomFeedItem)feed.Items.First();
                Console.WriteLine(atomFeedItem.Author);

                if (atomFeedItem.PublishedDate > rssFeed.LastProcessedItem)
                {
                    feed.Items.Remove(atomFeedItem);
                }
                else
                {
                    switch (rssFeed.FeedType)
                    {
                        case "RedditPost":
                            var matterMessage = new RedditPost(atomFeedItem);
                            break;
                        case "RedditInbox":
                            break;
                        default:
                            break;
                                
                    }
                }
                
                
            }
            
            

            
            foreach (var item in feed.Items)
            {
                
                //Console.WriteLine((AtomFeedItem); 
            }
           // throw new NotImplementedException();
        }


        public static async Task<Feed> GetRssFeed(string url)
        {
            try
            {
                var feed = await FeedReader.ReadAsync(url);


                //var httpClient = new HttpClient();
                //var result = httpClient.GetAsync(url).Result;
                //var stream = result.Content.ReadAsStreamAsync().Result;
                //var itemXml = XElement.Load(stream);
                //var feedParser = FeedParser.Create(itemXml.ToString());

                //return feedParser.Parse();
                return null;
            }
            catch (Exception e)
            {
                //Problem getting the feed.
                Console.WriteLine($"Problem retrieving feed\n Exception Message: {e.Message}");
                return null;
            }

        }

        public static void PostStringToMattermost(string error)
        {
            var m = new MattermostMessage
            {
                Text = error,
                Channel = Config.BotChannelDefault,
                Username = Config.BotNameDefault
            };


            PostToMattermost(m);
        }

        public static void PostToMattermost(MattermostMessage message)
        {
            var mc = new MatterhookClient(Config.MattermostWebhookUrl);
            Task.WaitAll(mc.PostAsync(message));
        }
    }
}
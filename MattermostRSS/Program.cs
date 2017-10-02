using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using CodeHollow.FeedReader;
//using CodeKoenig.SyndicationToolbox;
using Newtonsoft.Json;
using Matterhook.NET.MatterhookClient;
//using Feed = CodeKoenig.SyndicationToolbox.Feed;

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
                        ProcessRssFeeds(feed);
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

        private static void  ProcessRssFeeds(RssFeed rssFeed)
        {
            //Below method is obsolete. TODO: Update to use ReadAsync (quickfix for now)
            var feed = FeedReader.Read(rssFeed.Url);

            if (feed == null)
            {
                Console.WriteLine($"{DateTime.Now}RSS feed returned null");
                return;
            }

            IEnumerable<FeedItem> results = feed.Items.Where(x => x.PublishingDate > rssFeed.LastProcessedItem)
                .OrderBy(x => x.PublishingDate);

            //IEnumerable<FeedArticle> results = feed.Articles.Where(x => x.Published > rssFeed.LastProcessedItem).OrderBy(x => x.Published);

            if (!results.Any()) return;


            var rssItems = new List<RssToMattermostMessage>();

            ////TODO: There is probably a much better way of doing this
            switch (rssFeed.FeedType)
            {
                case "RedditPost":
                    rssItems.AddRange(results.Select(fa => new RedditPost(fa, rssFeed.FeedPretext)));
                    break;
                case "RedditInbox":
                    rssItems.AddRange(results.Select(fa => new RedditInbox(fa, rssFeed.FeedPretext)));
                    break;
                default://Use Generic Feed
                    rssItems.AddRange(results.Select(fa => new Generic(fa, rssFeed.FeedPretext)));
                    break;
            }

            foreach (var item in rssItems)
            {

                item.Channel = rssFeed.BotChannelOverride == ""
                    ? Config.BotChannelDefault
                    : rssFeed.BotChannelOverride;

                item.Username = rssFeed.BotNameOverride == ""
                    ? Config.BotNameDefault
                    : rssFeed.BotNameOverride;

                item.IconUrl = rssFeed.BotImageOverride == ""
                    ? new Uri(Config.BotImageDefault)
                    : new Uri(rssFeed.BotImageOverride);

                if (!rssFeed.IncludeContent)
                {
                    foreach (var att in item.Attachments)
                    {
                        att.Text = "";
                    }
                }

                PostToMattermost(item);
                rssFeed.LastProcessedItem = item.FeedItem.PublishingDate;
                Config.Save(ConfigPath);
            }




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
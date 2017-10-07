using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Matterhook.NET.MatterhookClient;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Matterfeed.NET
{
    internal class Program
    {
        private const string ConfigPath = "/config/secrets.json";

        public static Config Config = new Config();

        private static void Main(string[] args)
        {
            Console.WriteLine($"{DateTime.Now} - Application Started.");
            try
            {
                var allTasks = new List<Task>();
                LoadConfig();
                if (Config.RssFeeds != null)
                {
                    var rssTask = RssFeedReader.PeriodicRssAsync(TimeSpan.FromMilliseconds(Config.BotCheckIntervalMs), Config.RssFeeds);
                    allTasks.Add(rssTask);
                }

                if (Config.RedditJsonFeeds != null)
                {
                    var redditTask = RedditJsonFeedReader.PeriodicRedditAsync(TimeSpan.FromMilliseconds(Config.BotCheckIntervalMs), Config.RedditJsonFeeds);
                    allTasks.Add(redditTask);
                }

                if (Config.TwitterFeed != null)
                {
                    var twitterTask = TwitterFeedReader.PeriodicTwitterAsync(TimeSpan.FromMilliseconds(Config.TwitterFeed.Interval),Config.TwitterFeed);
                    allTasks.Add(twitterTask);
                }
                
                Task.WaitAll(allTasks.ToArray());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
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


        public static async Task PostToMattermost(MattermostMessage message)
        {
            if (message.Channel == null) message.Channel = Config.BotChannelDefault;
            if (message.Username == null) message.Username = Config.BotNameDefault;
            if (message.IconUrl == null) message.IconUrl = new Uri(Config.BotImageDefault);
            var mc = new MatterhookClient(Config.MattermostWebhookUrl);

            var response = await mc.PostAsync(message);

            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception(response != null
                    ? $"Unable to post to Mattermost.{response.StatusCode}"
                    : $"Unable to post to Mattermost.");
            }

        }

        internal static void SaveConfigSection(List<RedditJsonFeed> redditFeeds)
        {
            Config.RedditJsonFeeds = redditFeeds;
            Config.Save(ConfigPath);
        }

        internal static void SaveConfigSection(TwitterFeed twitterFeed)
        {
            Config.TwitterFeed = twitterFeed;
            Config.Save(ConfigPath);
        }

        internal static void SaveConfigSection(List<RssFeed> rssFeeds)
        {
            Config.RssFeeds = rssFeeds;
            Config.Save(ConfigPath);
        }
    }
}
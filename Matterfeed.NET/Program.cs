using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Matterhook.NET.MatterhookClient;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Matterfeed.NET
{
    internal static class Program
    {
        private const string ConfigPath = "/config/secrets.json";

        private static Config _config = new Config();

        private static void Main(string[] args)
        {
            Console.WriteLine($"{DateTime.Now} - Application Started.");
            try
            {
                var allTasks = new List<Task>();
                LoadConfig();
                if (_config.RssFeedConfig != null)
                {
                    var rssTask = RssFeedReader.PeriodicRssAsync(_config.RssFeedConfig);
                    allTasks.Add(rssTask);
                }

                if (_config.RedditFeedConfig != null)
                {
                    var redditTask = RedditJsonFeedReader.PeriodicRedditAsync(_config.RedditFeedConfig);
                    allTasks.Add(redditTask);
                }

                if (_config.TwitterFeedConfig != null)
                {
                    var twitterTask = TwitterFeedReader.PeriodicTwitterAsync(_config.TwitterFeedConfig);
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
            {
                using (var file = File.OpenText(ConfigPath))
                {
                    var serializer = new JsonSerializer();
                    _config = (Config)serializer.Deserialize(file, typeof(Config));
                }
            }
            else
            {
                Console.WriteLine("No secrets.json found! I Have no idea what to do...");
                Environment.Exit(1);
            }
        }


        public static async Task PostToMattermost(MattermostMessage message)
        {
            if (message.Channel == null) { message.Channel = _config.BotChannelDefault; }
            if (message.Username == null) { message.Username = _config.BotNameDefault; }
            if (message.IconUrl == null) { message.IconUrl = new Uri(_config.BotImageDefault); }
            var mc = new MatterhookClient(_config.MattermostWebhookUrl);

            var response = await mc.PostAsync(message);

            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception(response != null
                    ? $"Unable to post to Mattermost.{response.StatusCode}"
                    : $"Unable to post to Mattermost.");
            }

        }

        internal static void SaveConfigSection(RedditFeedConfig redditFeedConfig)
        {
            _config.RedditFeedConfig = redditFeedConfig;
            _config.Save(ConfigPath);
        }

        internal static void SaveConfigSection(TwitterFeedConfig twitterFeedConfig)
        {
            _config.TwitterFeedConfig = twitterFeedConfig;
            _config.Save(ConfigPath);
        }

        internal static void SaveConfigSection(RssFeedConfig rssFeedConfig)
        {
            _config.RssFeedConfig = rssFeedConfig;
            _config.Save(ConfigPath);
        }
    }
}
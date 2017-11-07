using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Matterfeed.NET
{
    public class Config
    {
       

        public int BotCheckIntervalMs { get; set; } = 30000; //Default to 30 seconds, but configure it in the secrets file
        public string MattermostWebhookUrl { get; set; } = "";

        public string BotChannelDefault { get; set; } = "";
        public string BotNameDefault { get; set; } = "";
        public string BotImageDefault { get; set; } = "";

        public RssFeedConfig RssFeedConfig { get; set; }
        public RedditFeedConfig RedditFeedConfig { get; set; }
        public TwitterFeedConfig TwitterFeedConfig { get; set; }


        public void Save(string path)
        {
            // serialize JSON directly to a file
            using (var file = File.CreateText(path))
            {
                var serializer = new JsonSerializer {Formatting = Formatting.Indented};
                serializer.Serialize(file, this);
            }
        }
    }

    public class RssFeedConfig
    {
        public int Interval { get; set; } = 300000;

        public List<RssFeed> RssFeeds { get; set; }
    }

    public class RssFeed
    {
        public bool FallbackMode { get; set; } = false;
        public string FeedPretext { get; set; }
        
        public string Url { get; set; }

        public string BotChannelOverride { get; set; } = "";
        public string BotNameOverride { get; set; } = "";
        public string BotImageOverride { get; set; } = "";

        public bool IncludeContent { get; set; } = true;
       
        public DateTime? LastProcessedItem { get; set; } = new DateTime();
    }

    public class RedditFeedConfig
    {
        public int Interval { get; set; } = 60000;
        public List<RedditJsonFeed> RedditJsonFeeds { get; set; }
    }

    public class RedditJsonFeed
    {
        public string FeedPretext { get; set; }
        public string Url { get; set; }
        public string BotChannelOverride { get; set; } = "";
        public string BotNameOverride { get; set; } = "";
        public string BotImageOverride { get; set; } = "";
        public DateTime? LastProcessedItem { get; set; } = new DateTime();
    }

    public class TwitterFeedConfig
    {
        public int Interval { get; set; } = 60000;
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
        public string AccessToken { get; set; }
        public string AccessTokenSecret { get; set; }
        

        public List<TwitterSearch> Searches { get; set; }
    }

    public class TwitterSearch
    {
        public string FeedPretext { get; set; }
        public string SearchTerm { get; set; }
        public string BotChannelOverride { get; set; } = "";
        public string BotNameOverride { get; set; } = "";
        public string BotImageOverride { get; set; } = "";
        public long LastProcessedId { get; set; } = 0;
    }
}
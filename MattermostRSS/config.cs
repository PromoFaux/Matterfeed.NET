using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace MattermostRSS
{
    public class Config
    {
       

        public int BotCheckIntervalMs { get; set; } = 30000; //Default to 30 seconds, but configure it in the secrets file
        public string MattermostWebhookUrl { get; set; } = "";

        public string BotChannelDefault { get; set; } = "";
        public string BotNameDefault { get; set; } = "";
        public string BotImageDefault { get; set; } = "";

        public List<RssFeed> RssFeeds { get; set; }

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

    public class RssFeed
    {
        public string FeedPretext { get; set; }
        
        public string Url { get; set; }

        public string BotChannelOverride { get; set; } = "";
        public string BotNameOverride { get; set; } = "";
        public string BotImageOverride { get; set; } = "";

        public bool IncludeContent { get; set; } = true;

        public string FeedSource { get; set; } = "";
        public string FeedType { get; set; }
        public DateTime? LastProcessedItem { get; set; } = new DateTime();
    }



}
using System;
using System.IO;
using Newtonsoft.Json;

namespace MattermostRSS
{
    public class ProcessedFeeds
    {
        public DateTime RedditInboxSubMention { get; set; } = new DateTime();

        //public DateTime RedditInboxPostReply { get; set; } = new DateTime();
        public DateTime RedditNewPostInPihole { get; set; } = new DateTime();

        public DateTime RedditFromSearch { get; set; } = new DateTime();
    }

    internal class LastProcessed
    {


        public LastProcessed(string path)
        {
            if (File.Exists(path))
                using (var file = File.OpenText(path))
                {
                    var serializer = new JsonSerializer();
                    Feeds = (ProcessedFeeds) serializer.Deserialize(file, typeof(ProcessedFeeds));
                }
            else
                Feeds = new ProcessedFeeds();
        }

        public ProcessedFeeds Feeds { get; }

        public void Save(string path)
        {
            // serialize JSON directly to a file
            using (var file = File.CreateText(path))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(file, Feeds);
            }
        }
    }
}
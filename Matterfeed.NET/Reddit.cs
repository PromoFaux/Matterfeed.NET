using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Matterfeed.NET
{
    public class RedditJson
    {
        [JsonProperty("data")]
        public RedditJsonData RedditJsonData { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }
    }

    public class RedditJsonData
    {
        [JsonProperty("before")]
        public object Before { get; set; }

        [JsonProperty("modhash")]
        public string Modhash { get; set; }

        [JsonProperty("after")]
        public string After { get; set; }

        [JsonProperty("children")]
        public RedditJsonChild[] RedditJsonChildren { get; set; }

        [JsonProperty("whitelist_status")]
        public string WhitelistStatus { get; set; }
    }

    public class RedditJsonChild
    {
        [JsonProperty("data")]
        public RedditJsonChildData Data { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }
    }

    public class RedditJsonChildData
    {
        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("created")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime? Created { get; set; }      

        [JsonProperty("created_utc")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime? CreatedUtc { get; set; }

        [JsonProperty("domain")]
        public string Domain { get; set; }

        [JsonProperty("is_video")]
        public bool? IsVideo { get; set; }

        [JsonProperty("is_self")]
        public bool? IsSelf { get; set; }

        [JsonProperty("media")]
        public object Media { get; set; }

        [JsonProperty("permalink")]
        public string Permalink { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
       
        [JsonProperty("post_hint")]
        public string PostHint { get; set; }

        [JsonProperty("selftext")]
        public string Selftext { get; set; }

        [JsonProperty("subreddit")]
        public string Subreddit { get; set; }

        [JsonProperty("thumbnail")]
        public string Thumbnail { get; set; }

        [JsonProperty("subreddit_name_prefixed")]
        public string SubredditNamePrefixed { get; set; }

        [JsonProperty("thumbnail_width")]
        public long? ThumbnailWidth { get; set; }

        [JsonProperty("thumbnail_height")]
        public long? ThumbnailHeight { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("distinguished")]
        public object Distinguished { get; set; }

        [JsonProperty("context")]
        public string Context { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

       

        [JsonProperty("body_html")]
        public string BodyHtml { get; set; }

       


        [JsonProperty("dest")]
        public string Dest { get; set; }

        [JsonProperty("likes")]
        public object Likes { get; set; }

        [JsonProperty("parent_id")]
        public object ParentId { get; set; }

        [JsonProperty("first_message_name")]
        public object FirstMessageName { get; set; }

        [JsonProperty("first_message")]
        public object FirstMessage { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("new")]
        public bool New { get; set; }


        [JsonProperty("num_comments")]
        public object NumComments { get; set; }

        [JsonProperty("score")]
        public long Score { get; set; }

        

        [JsonProperty("replies")]
        public string Replies { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

     

        [JsonProperty("was_comment")]
        public bool WasComment { get; set; }
    }

  


    public class UnixDateTimeConverter : DateTimeConverterBase
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.Float)
            {
                return reader.Value;
            }

            var ticks = Convert.ToInt64(reader.Value);

            var date = new DateTime(1970, 1, 1);
            date = date.AddSeconds(ticks);

            return date;
        }

        public override void WriteJson(JsonWriter writer, object value,
            JsonSerializer serializer)
        {
            long ticks;
            if (value is DateTime)
            {
                var epoc = new DateTime(1970, 1, 1);
                var delta = ((DateTime)value) - epoc;
                if (delta.TotalSeconds < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        "Unix epoc starts January 1st, 1970");
                }
                ticks = (long)delta.TotalSeconds;
            }
            else
            {
                throw new Exception("Expected date object value.");
            }
            writer.WriteValue(ticks);
        }
    }

}
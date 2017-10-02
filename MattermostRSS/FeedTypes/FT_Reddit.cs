using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeHollow.FeedReader;
using Matterhook.NET.MatterhookClient;
//using ReverseMarkdown;
using HtmlAgilityPack;
using ReverseMarkdown;
using CodeHollow.FeedReader.Feeds;

namespace MattermostRSS
{
    /// <summary>
    ///  A class for formatting Reddit Posts to Mattermost
    /// </summary>
    public class RedditPost : RssToMattermostMessage
    {
        private AtomFeedItem atomFeedItem;

        public RedditPost(AtomFeedItem atomFeedItem)
        {
            this.atomFeedItem = atomFeedItem;
        }

        public RedditPost(FeedItem fi, string preText)
        {
            FeedItem = fi;
            var author = fi.Author;
            var authorUrl = $"https://reddit.com{author}";
            var title = fi.Title;
            var titleUrl = fi.Link;
            string content;
            var subReddit = "";// fi.Categories.Count > 0 ? fi.Categories: "";

            var resultat = new HtmlDocument();
            resultat.LoadHtml(fi.Content);

            //Check to see if it is a self post or a link post
            var selfPost = resultat.DocumentNode.Descendants("div")
                .Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value.Contains("md")).ToList();

            var converter = new Converter();
            if (selfPost.Any())
            {
                content = converter.Convert(selfPost[0].InnerHtml).Replace("(/u/", "(https://reddit.com/u/")
                    .Replace("(/r/", "(https://reddit.com/r/");
            }
            else //we probably have a link post as there is no div with class "md"
            {
                var link = resultat.DocumentNode.Descendants("a")
                    .First(x => x.ParentNode.Name == "span" && x.InnerText == "[link]").Attributes["href"].Value;

                content = $"Linked Content: {link}";


                //TODO: Handle various types of link
                //if (link.Contains("imgur.com/a/")) //it's an imgur album link, we need the individual image URLs
                //{
                //    //Needs Auth.
                //    var i = link.LastIndexOf('/') +1;
                //    var albumID = link.Substring(i ,link.Length -i);

                //}


                // Content = converter.Convert(linkPost.OuterHtml);
            }


            Attachments = new List<MattermostAttachment>
            {
                new MattermostAttachment
                {
                    Pretext = subReddit == "" ? $"{preText}" : $"There is a new post in {subReddit}", //pretext overridden if we know the subreddit it's coming from
                    Fallback = subReddit == "" ? $"{preText}" : $"There is a new post in {subReddit}",
                    Title = title,
                    TitleLink = new Uri(titleUrl),
                    Text = $"{content}",
                    AuthorName = author,
                    AuthorLink = new Uri(authorUrl)
                }
            };

            title = Regex.Replace(title.Replace(" ", "-"), "[^0-9a-zA-Z-]+", "");
            Text = $"#{title}";
        }
    }

    /// <summary>
    /// A class for formatting Reddit inbox messages
    /// </summary>
    public class RedditInbox : RssToMattermostMessage
    {
        public RedditInbox(FeedItem fi, string preText)
        {
            var resultat = new HtmlDocument();
            resultat.LoadHtml(fi.Content);

            FeedItem = fi;

            var author = fi.Author;
            var authorUrl = $"https://reddit.com{author}";

            var colon = fi.Title.IndexOf(':');
            var title = $"{fi.Title.Substring(colon + 2, fi.Title.Length - colon - 2)}";

            var pElements = resultat.DocumentNode.Descendants().Where(x => x.Name == "p").ToList();

            var converter = new Converter();
            var comment = converter.Convert(pElements.Aggregate("", (current, p) => current + $"{p.InnerHtml}<br/>").Replace("\"/u/", "\"https://reddit.com/u/")
                .Replace("\"/r/", "\"https://reddit.com/r/"));

            var commentUrl = fi.Link;

            Attachments = new List<MattermostAttachment>
            {
                new MattermostAttachment
                {
                    Pretext = $"{preText}",
                    Title = title,
                    TitleLink = new Uri(commentUrl),
                    Text = $"{comment}" ,
                    AuthorName = author,
                    AuthorLink = new Uri(authorUrl)
                }
            };
            title = Regex.Replace(title.Replace(" ", "-"), "[^0-9a-zA-Z-]+", "");
            Text = $"#{title}";
        }
    }


    public class Rootobject
    {
        public string kind { get; set; }
        public Data data { get; set; }
    }

    public class Data
    {
        public string modhash { get; set; }
        public string whitelist_status { get; set; }
        public Child[] children { get; set; }
        public string after { get; set; }
        public object before { get; set; }
    }

    public class Child
    {
        public string kind { get; set; }
        public Data1 data { get; set; }
    }

    public class Data1
    {
        public string domain { get; set; }
        public object approved_at_utc { get; set; }
        public object banned_by { get; set; }
        public Media_Embed media_embed { get; set; }
        public int? thumbnail_width { get; set; }
        public string subreddit { get; set; }
        public string selftext_html { get; set; }
        public string selftext { get; set; }
        public object likes { get; set; }
        public object suggested_sort { get; set; }
        public object[] user_reports { get; set; }
        public object secure_media { get; set; }
        public string link_flair_text { get; set; }
        public string id { get; set; }
        public object banned_at_utc { get; set; }
        public object view_count { get; set; }
        public bool archived { get; set; }
        public bool clicked { get; set; }
        public object report_reasons { get; set; }
        public string title { get; set; }
        public int num_crossposts { get; set; }
        public bool saved { get; set; }
        public object[] mod_reports { get; set; }
        public bool can_mod_post { get; set; }
        public bool is_crosspostable { get; set; }
        public bool pinned { get; set; }
        public int score { get; set; }
        public object approved_by { get; set; }
        public bool over_18 { get; set; }
        public bool hidden { get; set; }
        public string thumbnail { get; set; }
        public string subreddit_id { get; set; }
        public object edited { get; set; }
        public string link_flair_css_class { get; set; }
        public object author_flair_css_class { get; set; }
        public bool contest_mode { get; set; }
        public int gilded { get; set; }
        public int downs { get; set; }
        public bool brand_safe { get; set; }
        public Secure_Media_Embed secure_media_embed { get; set; }
        public object removal_reason { get; set; }
        public object author_flair_text { get; set; }
        public bool stickied { get; set; }
        public bool can_gild { get; set; }
        public int? thumbnail_height { get; set; }
        public string parent_whitelist_status { get; set; }
        public string name { get; set; }
        public bool spoiler { get; set; }
        public string permalink { get; set; }
        public string subreddit_type { get; set; }
        public bool locked { get; set; }
        public bool hide_score { get; set; }
        public int created { get; set; }
        public string url { get; set; }
        public string whitelist_status { get; set; }
        public bool quarantine { get; set; }
        public string author { get; set; }
        public int created_utc { get; set; }
        public string subreddit_name_prefixed { get; set; }
        public int ups { get; set; }
        public object media { get; set; }
        public int num_comments { get; set; }
        public bool is_self { get; set; }
        public bool visited { get; set; }
        public object num_reports { get; set; }
        public bool is_video { get; set; }
        public object distinguished { get; set; }
        public Preview preview { get; set; }
        public string post_hint { get; set; }
    }

    public class Media_Embed
    {
    }

    public class Secure_Media_Embed
    {
    }

    public class Preview
    {
        public Image[] images { get; set; }
        public bool enabled { get; set; }
    }

    public class Image
    {
        public Source source { get; set; }
        public Resolution[] resolutions { get; set; }
        public Variants variants { get; set; }
        public string id { get; set; }
    }

    public class Source
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class Variants
    {
    }

    public class Resolution
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }




}

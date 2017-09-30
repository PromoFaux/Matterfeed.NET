using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeHollow.FeedReader;
using Html2Markdown;
using Matterhook.NET.MatterhookClient;
//using ReverseMarkdown;
using HtmlAgilityPack;


namespace MattermostRSS
{
    /// <summary>
    ///  A class for formatting Reddit Posts to Mattermost
    /// </summary>
    public class RedditPost : RssToMattermostMessage
    {
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
            var comment = converter.Convert(pElements.Aggregate("", (current, p) => current + $"{p.InnerHtml}\n").Replace("\"/u/", "\"https://reddit.com/u/")
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
}

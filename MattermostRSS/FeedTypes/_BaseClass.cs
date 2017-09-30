using System;
using System.Collections.Generic;
using System.Linq;
using CodeHollow.FeedReader;
using Html2Markdown;
//using CodeKoenig.SyndicationToolbox;
using Matterhook.NET;
using Matterhook.NET.MatterhookClient;
//using ReverseMarkdown;


namespace MattermostRSS
{

    public class RssToMattermostMessage : MattermostMessage
    {
        public FeedItem FeedItem;
    }

    /// <summary>
    /// Generic class to format RSS Feed items to Mattermost
    /// </summary>
    public class Generic : RssToMattermostMessage
    {
        public Generic(FeedItem fi, string preText)
        {
            var converter = new Converter();

            FeedItem = fi;

            Attachments = new List<MattermostAttachment>
            {
                new MattermostAttachment
                {
                    Pretext = preText,
                    Title = converter.Convert(fi.Title),
                    TitleLink = new Uri(fi.Link),
                    Text = converter.Convert(fi.Description),
                    AuthorName = fi.Author
                }
            };
          
        }
    }

}
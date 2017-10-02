using System;
using System.Collections.Generic;
using CodeHollow.FeedReader;
using Matterhook.NET.MatterhookClient;
using ReverseMarkdown;

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
        public Generic(FeedItem fi, string preText, bool includeContent = true)
        {
            var converter = new Converter();

            FeedItem = fi;

            Attachments = new List<MattermostAttachment>
            {
                new MattermostAttachment
                {
                    Pretext = preText,
                    Title = converter.Convert(fi.Title??""),
                    TitleLink = new Uri(fi.Link??""),
                    Text = converter.Convert(includeContent? fi.Content??fi.Description??"No Content or Description":fi.Description??"No Description"),
                    AuthorName = fi.Author??""
                }
            };

        }
    }

}
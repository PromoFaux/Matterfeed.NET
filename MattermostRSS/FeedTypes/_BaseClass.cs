using System;
using System.Collections.Generic;
using CodeKoenig.SyndicationToolbox;
using Matterhook.NET.MatterhookClient;
using ReverseMarkdown;


namespace MattermostRSS
{

    public class RssToMattermostMessage : MattermostMessage
    {
        public DateTime PublishDate;
    }

    /// <summary>
    /// Generic class to format RSS Feed items to Mattermost
    /// </summary>
    public class Generic : RssToMattermostMessage
    {
        public Generic(FeedArticle fa, string preText)
        {
            var converter = new Converter();

            Attachments = new List<MattermostAttachment>
            {
                new MattermostAttachment
                {
                    Pretext = preText,
                    Title = converter.Convert(fa.Title ?? ""),
                    TitleLink = new Uri(fa.WebUri),
                    Text = converter.Convert(fa.Content ?? ""),
                    AuthorName = fa.Author
                }
            };

            PublishDate = fa.Published;
        }
    }

}
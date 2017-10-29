using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matterhook.NET.MatterhookClient;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace Matterfeed.NET
{
    internal static class TwitterFeedReader
    {
        internal static async Task PeriodicTwitterAsync(TimeSpan interval, TwitterFeed twitterFeed)
        {
            while (true)
            {
                var sbOut = new StringBuilder();
                sbOut.Append($"\n{DateTime.Now}\nTwitter Feed Reader.");

                Auth.SetUserCredentials(twitterFeed.ConsumerKey, twitterFeed.ConsumerSecret,twitterFeed.AccessToken, twitterFeed.AccessTokenSecret);

                var authenticatedUser = User.GetAuthenticatedUser();
                sbOut.Append($"\nAuthenticated Twitter User: {authenticatedUser}");
                
                if (twitterFeed.Searches.Any())
                {
                    foreach (var s in twitterFeed.Searches)
                    {
                        var procCount = 0;
                        sbOut.Append($"\nProcessing Search: {s.SearchTerm}");
                        var searchParameter = new SearchTweetsParameters(s.SearchTerm)
                        {
                            SearchType = SearchResultType.Mixed,
                            MaximumNumberOfResults = 100,
                            SinceId = s.LastProcessedId
                        };

                        var tweets = Search.SearchTweets(searchParameter);

                        var rateLimits = RateLimit.GetCurrentCredentialsRateLimits();
                        sbOut.Append($"\nRate Limit info: {rateLimits.SearchTweetsLimit}");

                        foreach (var t in tweets.OrderBy(x => x.Id))
                        {
                            var message = new MattermostMessage
                            {
                                Channel = s.BotChannelOverride ==""?null:s.BotChannelOverride,
                                Username =s.BotNameOverride ==""? null:s.BotNameOverride,
                                IconUrl = s.BotImageOverride == "" ? null : new Uri(s.BotImageOverride),
                                Attachments = new List<MattermostAttachment>
                                {
                                    new MattermostAttachment
                                    {
                                        Pretext = $"Tweet by [@{t.CreatedBy.ScreenName}](https://twitter.com/{t.CreatedBy.ScreenName}) at [{t.CreatedAt:HH:mm d MMM yyyy} UTC]({t.Url})",
                                        Text = $">{t.FullText}",
                                        AuthorName = t.CreatedBy.Name ?? "",
                                        AuthorLink = new Uri($"https://twitter.com/{t.CreatedBy.ScreenName}"),
                                        AuthorIcon = new Uri(t.CreatedBy.ProfileImageUrl400x400 ?? "")
                                    }
                                }
                            };

                            try
                            {
                                await Program.PostToMattermost(message);
                                s.LastProcessedId = t.Id;
                                procCount++;
                            }
                            catch (Exception e)
                            {
                                sbOut.Append($"\nException: {e.Message}");
                            }
                            
                        }
                        sbOut.Append($"\nProcessed {procCount}/{tweets.Count()} tweets");
                    }
                }
                Console.WriteLine(sbOut.ToString());
                Program.SaveConfigSection(twitterFeed);
                await Task.Delay(interval).ConfigureAwait(false);
            }
        }
    }
}

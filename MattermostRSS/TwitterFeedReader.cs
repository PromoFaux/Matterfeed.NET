using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Matterhook.NET.MatterhookClient;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace MattermostRSS
{
    internal class TwitterFeedReader
    {
        internal static async Task PeriodicTwitterAsync(TimeSpan interval, TwitterFeed twitterFeed)
        {
            while (true)
            {
                var retval = $"\n{DateTime.Now}\nTwitter Feed Reader.";

                Auth.SetUserCredentials(twitterFeed.ConsumerKey, twitterFeed.ConsumerSecret,twitterFeed.AccessToken, twitterFeed.AccessTokenSecret);

                var authenticatedUser = User.GetAuthenticatedUser();
                retval += $"\nAuthenticated Twitter User: {authenticatedUser}";
                
                if (twitterFeed.Searches.Any())
                {
                    foreach (var s in twitterFeed.Searches)
                    {
                        var procCount = 0;
                        retval += $"\nProcessing Search: {s.SearchTerm}";
                        var searchParameter = new SearchTweetsParameters(s.SearchTerm)
                        {
                            SearchType = SearchResultType.Mixed,
                            MaximumNumberOfResults = 100,
                            SinceId = s.LastProcessedId
                        };

                        var tweets = Search.SearchTweets(searchParameter);

                        var rateLimits = RateLimit.GetCurrentCredentialsRateLimits();
                        retval +=$"\nRate Limit info: {rateLimits.SearchTweetsLimit}";

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
                                        Pretext = s.FeedPretext,
                                        Title = "New Tweet from Search Result",
                                        TitleLink = new Uri(t.Url ?? ""),
                                        Text = $">{t.FullText ?? ""}",
                                        AuthorName = t.CreatedBy.Name ?? "",
                                        AuthorLink = t.CreatedBy.Url == null ? null : new Uri(t.CreatedBy.Url),
                                        AuthorIcon = new Uri(t.CreatedBy.ProfileImageUrl400x400 ?? "")
                                    }
                                }
                            };

                            try
                            {
                                //Task.WaitAll(Program.PostToMattermost(message));
                                await Program.PostToMattermost(message);
                                s.LastProcessedId = t.Id;
                                procCount++;
                            }
                            catch (Exception e)
                            {
                                retval += $"\nException: {e.Message}";
                            }
                            
                        }
                        retval += $"\nProcessed {procCount}/{tweets.Count()} tweets";
                    }
                }
                Console.WriteLine(retval);
                Program.SaveConfigSection(twitterFeed);
                await Task.Delay(interval);
            }
        }
    }
}

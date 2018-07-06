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
        internal static async Task PeriodicTwitterAsync(TwitterFeedConfig twitterFeedConfig)
        {
            while (true)
            {
                var logit = false;
                var sbOut = new StringBuilder();
                try
                {
                   
                    sbOut.Append($"\n{DateTime.Now}\nTwitter Feed Reader.");

                    Auth.SetUserCredentials(twitterFeedConfig.ConsumerKey, twitterFeedConfig.ConsumerSecret, twitterFeedConfig.AccessToken, twitterFeedConfig.AccessTokenSecret);

                    //Check the authentication tokens are correct by getting the authenticated username
                    var authenticatedUser = User.GetAuthenticatedUser();
                    if (authenticatedUser != null)
                    {
                        sbOut.Append($"\nAuthenticated Twitter User: {authenticatedUser}");

                        //Get the rate limits for the authenticated user
                        var rateLimits = RateLimit.GetCurrentCredentialsRateLimits();

                        if (twitterFeedConfig.Searches.Any())
                        {
                            if (rateLimits.SearchTweetsLimit.Remaining > 0)
                            {
                                sbOut.Append($"\nRate Limit info: {rateLimits.SearchTweetsLimit}");

                                foreach (var s in twitterFeedConfig.Searches)
                                {
                                    sbOut.Append($"\nProcessing Search: {s.SearchTerm}");
                                    var searchParameter = new SearchTweetsParameters(s.SearchTerm)
                                    {
                                        SearchType = SearchResultType.Mixed,
                                        MaximumNumberOfResults = 100,
                                        SinceId = s.LastProcessedId
                                    };

                                    var tweets = Search.SearchTweets(searchParameter).ToList();
                                    
                                    foreach (var t in tweets.OrderBy(x => x.Id))
                                    {
                                        var message = new MattermostMessage
                                        {
                                            Channel = s.BotChannelOverride == "" ? null : s.BotChannelOverride,
                                            Username = s.BotNameOverride == "" ? null : s.BotNameOverride,
                                            IconUrl = s.BotImageOverride == "" ? null : new Uri(s.BotImageOverride),
                                            Attachments = new List<MattermostAttachment>
                                            {
                                                new MattermostAttachment
                                                {
                                                    Pretext =
                                                        $"Tweet by [@{t.CreatedBy.ScreenName}](https://twitter.com/{t.CreatedBy.ScreenName}) at [{t.CreatedAt:HH:mm d MMM yyyy} UTC]({t.Url})",
                                                    Text = $">{t.FullText}",
                                                    AuthorName = t.CreatedBy.Name ?? "",
                                                    AuthorLink =
                                                        new Uri($"https://twitter.com/{t.CreatedBy.ScreenName}"),
                                                    AuthorIcon = new Uri(t.CreatedBy.ProfileImageUrlHttps ?? "")
                                                }
                                            }
                                        };

                                        try
                                        {
                                            await Program.PostToMattermost(message);
                                            s.LastProcessedId = t.Id;
                                        }
                                        catch (Exception e)
                                        {
                                            sbOut.Append($"\nException: {e.Message}");
                                            logit = true;
                                            //assume there is an issue with Mattermost, log the error and fall out of the foreach
                                            break;
                                        }
                                    }
                                    if (logit)
                                    {
                                        break;
                                    }

                                    sbOut.Append($"\nProcessed Search: {s.SearchTerm}");
                                }
                            }
                            else
                            {
                                sbOut.Append($"\nERROR: Search Rate limit hit! Limit resets at {rateLimits.SearchTweetsLimit.ResetDateTime}");
                                logit = true;
                            }
                        }
                        else
                        {
                            sbOut.Append($"\nERROR: No search terms defined");
                            logit = true;
                        }
                    }
                    else
                    {
                        sbOut.Append($"\nERROR: Not authenticated! Check credentials.");
                        logit = true;
                    }

                    if (logit)
                    {
                        Console.WriteLine(sbOut.ToString());
                    }

                    Program.SaveConfigSection(twitterFeedConfig);
                    await Task.Delay(TimeSpan.FromMilliseconds(twitterFeedConfig.Interval)).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    sbOut.Append($"\nException: {e}");
                    Console.WriteLine(sbOut.ToString());
                    await Task.Delay(TimeSpan.FromMilliseconds(twitterFeedConfig.Interval)).ConfigureAwait(false);
                }
               
            }
        }
    }
}

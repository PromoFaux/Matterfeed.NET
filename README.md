# MattermostRSS
Parse RSS Feeds and post them to your Mattermost server!

This is a simple bot, mainly written to improve my .NET Core and Docker experience, and to replace our reliance on IFTTT over at [Pi-hole](https://github.com/pi-hole/), open sourced because it may be useful to others.

## Deployment

- Clone the repo to your machine (known from this point on as `${RepoDir}`)
- Create the config file in: `${RepoDir}/MattermostRSS/config/` (Here you will find a `secrets.json.sample` to give you the framework of the file - More details below)
- Once the config file is created, start the bot:
```
cd ${RepoDir}/MattermostRSS/
docker-compose -f docker-compose.ci.build.yml up
docker-compose up -d --build
```

## Configuration

[Example Config file](https://github.com/PromoFaux/MattermostRSS/blob/master/MattermostRSS/config/secrets.json.sample)

Each `RssFeeds` element can optionally override the default Channel/Bot Name/Bot Icon using the following properties:

`BotChannelOverride`

`BotNameOverride`

`BotImageOverride`

Currently `FeedType` accepts the values `RedditPost` and `RedditInbox`(The ones I personally need so far!), or you can leave it blank for a generic feed. 

`LastProcessedItem` is automatically added and saved once the bot has looped through all of it's RSS feeds. 

Example Config and screenshot of output:

```JSON
{
  "BotCheckIntervalMs":30000,
  "MattermostWebhookUrl": "https://yourmattermostserver.com/hooks/kjnk4j3wnfkse",
  "BotChannelDefault": "Rss-Feeds",
  "BotNameDefault": "Adam's Marvelous RSS Bot",
  "BotImageDefault": "https://whatever.com/image.jpg",
  "RssFeeds":[  
    {  
       "FeedPretext":"New post in /r/pihole!",
       "Url":"https://www.reddit.com/r/pihole/new.rss",
       "BotChannelOverride":"",
       "BotNameOverride":"",
       "BotImageOverride":"",
       "FeedType":"RedditPost",
       "LastProcessedItem":"2017-08-01T19:57:39+00:00"
    },
  ]
}
```
![](https://i.imgur.com/r1RyHlg.png)

## Extending FeedTypes

If the generic feed does not handle your message as well as you would like, you can write your own FeedType to format the RSS Feed as you would like to see it.

To do so, simply add a new class to [FeedTypes.cs](https://github.com/PromoFaux/MattermostRSS/blob/master/MattermostRSS/MattermostRSS/FeedTypes.cs) inheriting the class `RssToMattermostMessage`, and then adding it to [the switch statement in Program.cs](https://github.com/PromoFaux/MattermostRSS/blob/master/MattermostRSS/MattermostRSS/Program.cs#L78-L89)

Example of a feed that needs formatting: https://news.google.com/?output=rss
![](https://i.imgur.com/MnIQYZC.png)

If you do come up with any of your own, please consider submitting a Pull Request with your new FeedTypes to help improve the project

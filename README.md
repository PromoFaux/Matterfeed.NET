# Matterfeed.NET
Parse various feeds and post them to your mattermost server!

So far, this bot will parse:
- RSS
- Reddit JSON Feeds
- Twitter (Search only for now)

[![Docker Build Status](https://img.shields.io/docker/build/promofaux/matterfeed.net.svg)](https://hub.docker.com/r/promofaux/matterfeed.net/builds/) [![Docker Stars](https://img.shields.io/docker/stars/promofaux/matterfeed.net.svg)](https://hub.docker.com/r/promofaux/matterfeed.net/) [![Docker Pulls](https://img.shields.io/docker/pulls/promofaux/matterfeed.net.svg)](https://hub.docker.com/r/promofaux/matterfeed.net/) 

## Deployment
Recommended - Use pre-built container:

- Create a directory to store the bot's config file, e.g `/opt/bot/Matterfeed.NET` (`${YOUR_DIRECTORY}`)
- Create the config file in `${YOUR_DIRECTORY}`. See [Example Config file](https://github.com/PromoFaux/Matterfeed.NET/blob/master/config/secrets.json.sample) for details, or read below.
- `docker run -d --restart=always -v ${YOUR_DIRECTORY}/:/config/ --name Matterfeed.NET promofaux/matterfeed.net`


Make sure that Docker has permission to read and write to the config directory so that it can write changes to `secrets.json`

----

Alternative - build the container yourself:
- Clone the repo to your machine (known from this point on as `${RepoDir}`)
- Create the config file in: `${RepoDir}/Matterfeed.NET/config/` (Here you will find a `secrets.json.sample` to give you the framework of the file - More details below)
- Once the config file is created, start the bot:
```
cd ${RepoDir}/Matterfeed.NET/
docker-compose -f docker-compose.ci.build.yml up
docker-compose up -d --build
```

## Configuration

[Example Config file](https://github.com/PromoFaux/Matterfeed.NET/blob/master/Matterfeed.NET/config/secrets.json.sample)

Each `RssFeeds` element can optionally override the default Channel/Bot Name/Bot Icon using the following properties:

`BotChannelOverride`

`BotNameOverride`

`BotImageOverride`

`IncludeContent` - Defaults to `true`. Set to false to exclude feed content

`LastProcessedItem` is automatically added and saved once the bot has succesfully posted it to Mattermost

`FallbackMode` will store a copy of the feed and use that to compare against new feed grabs. This is particularly useful when feeds don't use the PubDate field properly.


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
        "FallbackMode": false
        "FeedPretext": "Schneier on Security",
        "Url": "https://www.schneier.com/blog/atom.xml",
        "BotChannelOverride": "",
        "BotNameOverride": "Bruce Schneier",
        "BotImageOverride": "https://www.schneier.com/images/bruce-blog3.jpg",
        "IncludeContent": true
    },
    ]
}
```

## Reddit JSON Feeds


```JSON
    "RedditJsonFeeds": [
    {
        "FeedPretext": "New reddit post from search result!",
        "Url": "https://www.reddit.com/search.json?q=Pi-hole+OR+pihole+NOT+subreddit%3Apihole",
        "BotChannelOverride": "Reddit",
        "BotNameOverride": "",
        "BotImageOverride": "https://i.imgur.com/3NtinwD.png"
    }
    ]
```

## Twitter Search

First, you will need to [obtain twitter application credentials](https://apps.twitter.com/). Once you have those, you can add them to a `Twitterfeed` configuration object.

Do not set the interval to be too small, as you may hit the API limits imposed by Twitter.

```JSON
 "TwitterFeed": {
    "Interval": 120000,
    "ConsumerKey": "Your Consumer Key",
    "ConsumerSecret": "Your Consumer Secret",
    "AccessToken": "Your Access Token",
    "AccessTokenSecret": "Your Access Token Secret",
    "Searches": [
      {
        "FeedPretext": "Twitter search: Pi-hole",
        "SearchTerm": "\"pihole\" OR \"pi AND hole\" OR \"pi-hole\" -\"shut\" OR \"@The_Pi_Hole\" -from:@A_Pi_Hole -from:@My_Pi_Hole",
        "BotChannelOverride": "",
        "BotNameOverride": "Terrence the Twitter bot",
        "BotImageOverride": "",
        "LastProcessedId": 0
      }
    ]
  
```


#### Third Party Libraries

This project wouldn't be anywhere near as far ahead if it wasn't for the use of some excellent nuget packages. In no particular order:

[ReverseMarkdown](https://github.com/mysticmind/reversemarkdown-net) by [@mysticmind](https://github.com/mysticmind)

[FeedReader](https://github.com/codehollow/FeedReader/) by [@codehollow](https://github.com/codehollow)

[Tweetinvi](https://github.com/linvi/tweetinvi) by [@linvi](https://github.com/linvi)




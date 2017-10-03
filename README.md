# MattermostRSS
Parse RSS Feeds and post them to your Mattermost server!

This is a simple bot, mainly written to improve my .NET Core and Docker experience, and to replace our reliance on IFTTT over at [Pi-hole](https://github.com/pi-hole/), open sourced because it may be useful to others.

In addition to RSS, the bot will also parse Reddit JSON feeds. 

[![Docker Build Status](https://img.shields.io/docker/build/promofaux/mattermostrss.svg)](https://hub.docker.com/r/promofaux/mattermostrss/builds/) [![Docker Stars](https://img.shields.io/docker/stars/promofaux/mattermostrss.svg)](https://hub.docker.com/r/promofaux/mattermostrss/) [![Docker Pulls](https://img.shields.io/docker/pulls/promofaux/mattermostrss.svg)](https://hub.docker.com/r/promofaux/mattermostrss/) 

## Deployment
Recommended - Use pre-built container:
- Create a directory to store the bot's config file, e.g `/opt/bot/MattermostRSS` (`${YOUR_DIRECTORY}`)
- Create the config file in `${YOUR_DIRECTORY}`. See [Example Config file](https://github.com/PromoFaux/MattermostRSS/blob/master/config/secrets.json.sample) for details, or read below.
- `docker run -d --restart=always -v ${YOUR_DIRECTORY}/:/config/ -e PUID=${USER} -e PGID=${GROUP} --name MattermostRSS promofaux/mattermostrss`

Make sure the config directory is owned by `${user}` and `${group}` so that the bot can read and write to the config file.

----

Alternative - build the container yourself:
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

`IncludeContent` - Defaults to `true`. Set to false to exclude feed content

`LastProcessedItem` is automatically added and saved once the bot has succesfully posted it to Mattermost


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

![](https://i.imgur.com/nW6fRsY.png)

## Reddit JSON Feeds

In addition to posting Generic RSS Feeds, I have also added the ability to parse and post messages from Reddit JSON Feeds

Just add a `RedditJsonFeeds` array to your config file:
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

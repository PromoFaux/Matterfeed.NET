To deploy:

Clone repo to the server. 

To build:
First you need to make sure `secrets.json` exists in `${Repo_Location}/MattermostRSS/MattermostRss` (TODO: sort out project directory structure). There is a `secrets.json.sample` to give you an idea.

```
cd ${Repo_Location}/MattermostRSS
docker-compose -f docker-compose.ci.build.yml up
docker-compose up -d
```

To see why it hasn't sent a message to Mattermost in a while (it's probably crashed)
`docker logs mattermostrss_mattermostrss_1`

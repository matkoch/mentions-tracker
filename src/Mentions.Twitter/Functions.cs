using System;
using System.Linq;
using System.Threading.Tasks;
using Mentions.Common;
using Mentions.Reddit;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Models.Entities;
using Tweetinvi.Parameters;

namespace Mentions.Twitter;

public class Functions
{
    public Functions(Configuration configuration)
    {
        Configuration = configuration;
    }

    public Configuration Configuration { get; }

    public const string Minutes = "15";

    [FunctionName(nameof(ScheduleSearches))]
    public async Task ScheduleSearches(
        [TimerTrigger($"0 */{Minutes} * * * *")] TimerInfo timer,
        [Queue(nameof(SlackAttachment))] IAsyncCollector<SlackAttachment> attachmentCollector,
        ILogger log)
    {
        var scheduleMinutes = TimeSpan.FromMinutes(int.Parse(Minutes));
        var before = timer.ScheduleStatus.Last != default
            ? timer.ScheduleStatus.Last.ToDateTimeOffset()
            : DateTimeOffset.UtcNow.Subtract(scheduleMinutes);
        var after = before.Subtract(scheduleMinutes);

        var twitterClient = new TwitterClient(
            consumerKey: Configuration.TwitterConsumerKey,
            consumerSecret: Configuration.TwitterConsumerSecret,
            accessToken: Configuration.TwitterAccessToken,
            accessSecret: Configuration.TwitterAccessTokenSecret);

        var tweets = await twitterClient.Search.SearchTweetsAsync(new SearchTweetsParameters(string.Empty)
        {
            Query = Configuration.Keywords.Single(),
            Since = after.UtcDateTime,
            Until = before.UtcDateTime
        });

        string GetUserLink(IUserMentionEntity x) => $"*<https://twitter.com/{x.ScreenName}|@{x.ScreenName}>*";

        SlackAttachment CreateAttachment(ITweet tweet)
        {
            return new SlackAttachment
            {
                Color = SlackClient.GetColor(
                    root: tweet.InReplyToStatusId == null,
                    knownUser: Configuration.KnownUsers.Contains(tweet.CreatedBy.ScreenName,
                        StringComparer.OrdinalIgnoreCase)),
                AuthorName = tweet.CreatedBy.Name,
                AuthorSubname = $"@{tweet.CreatedBy.ScreenName}",
                AuthorIcon = tweet.CreatedBy.ProfileImageUrl400x400,
                AuthorLink = tweet.Url,
                ImageUrl = tweet.Media.FirstOrDefault()?.MediaURLHttps,
                Text = tweet.Text,
                Footer = tweet.InReplyToStatusId != null
                    ? $"Replied to {string.Join(", ", tweet.UserMentions.Select(GetUserLink))}"
                    : null,
                FooterIcon = "https://raw.githubusercontent.com/matkoch/mentions-tracker/main/images/twitter.png",
                Ts = tweet.CreatedAt.ToUnixTimeSeconds()
            };
        }

        foreach (var tweet in tweets.OrderBy(x => x.CreatedAt))
        {
            var attachment = CreateAttachment(tweet);
            await attachmentCollector.AddAsync(attachment);
        }
    }

    [FunctionName(nameof(PostSlack))]
    public async Task PostSlack([QueueTrigger(nameof(SlackAttachment))] SlackAttachment attachment)
    {
        await SlackClient.Post(attachment, Configuration.SlackWebhook);
    }
}

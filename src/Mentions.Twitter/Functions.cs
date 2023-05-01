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
    private readonly Configuration _configuration;
    private readonly TwitterClient _twitterClient;
    private readonly TranslationClient _translationClient;
    private readonly SlackClient _slackClient;

    public Functions(
        Configuration configuration,
        TwitterClient twitterClient,
        TranslationClient translationClient,
        SlackClient slackClient)
    {
        _configuration = configuration;
        _twitterClient = twitterClient;
        _translationClient = translationClient;
        _slackClient = slackClient;
    }

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

        var tweets = await _twitterClient.Search.SearchTweetsAsync(new SearchTweetsParameters(string.Empty)
        {
            Query = _configuration.Keywords.Single(),
            Since = after.UtcDateTime,
            Until = before.UtcDateTime
        });

        string GetUserLink(IUserMentionEntity x) => $"*<https://twitter.com/{x.ScreenName}|@{x.ScreenName}>*";

        async Task<SlackAttachment> CreateAttachment(ITweet tweet)
        {
            return new SlackAttachment
            {
                Color = SlackClient.GetColor(
                    root: tweet.InReplyToStatusId == null,
                    knownUser: _configuration.KnownUsers.Contains(tweet.CreatedBy.ScreenName,
                        StringComparer.OrdinalIgnoreCase)),
                AuthorName = tweet.CreatedBy.Name,
                AuthorSubname = $"@{tweet.CreatedBy.ScreenName}",
                AuthorIcon = tweet.CreatedBy.ProfileImageUrl400x400,
                AuthorLink = tweet.Url,
                ImageUrl = tweet.Media.FirstOrDefault()?.MediaURLHttps,
                Text = (await _translationClient.Translate(tweet.Text) ?? tweet.Text).VisualizeProducts(),
                Footer = tweet.InReplyToStatusId != null
                    ? $"Replied to {tweet.UserMentions.Select(GetUserLink).Join(", ")}"
                    : null,
                FooterIcon = "https://raw.githubusercontent.com/matkoch/mentions-tracker/main/images/twitter.png",
                Ts = tweet.CreatedAt.ToUnixTimeSeconds()
            };
        }

        foreach (var tweet in tweets.OrderBy(x => x.CreatedAt))
        {
            var attachment = await CreateAttachment(tweet);
            await attachmentCollector.AddAsync(attachment);
        }
    }

    [FunctionName(nameof(PostSlack))]
    public async Task PostSlack([QueueTrigger(nameof(SlackAttachment))] SlackAttachment attachment)
    {
        await _slackClient.Post(attachment);
    }
}

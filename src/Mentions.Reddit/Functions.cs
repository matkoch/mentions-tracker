using System;
using System.Linq;
using System.Threading.Tasks;
using Markdig;
using Mentions.Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Mentions.Reddit;

public class Functions
{
    public Functions(Configuration configuration)
    {
        Keywords = configuration.Keywords;
        Subreddits = configuration.Subreddits;
        Exclusions = configuration.Exclusions;
        KnownUsers = configuration.KnownUsers;
        SlackWebhook = configuration.SlackWebhook;
    }

    public string[] Keywords { get; }
    public string[] Subreddits { get; }
    public string[] Exclusions { get; }
    public string[] KnownUsers { get; }
    public string SlackWebhook { get; }

    public const string Minutes = "15";

    public class Search
    {
        public string Keyword;
        public DateTimeOffset After;
        public DateTimeOffset Before;
    }

    [FunctionName(nameof(ScheduleSearches))]
    public async Task ScheduleSearches(
        [TimerTrigger($"0 */{Minutes} * * * *")] TimerInfo timer,
        [Queue(nameof(Search))] IAsyncCollector<Search> searchCollector,
        ILogger log)
    {
        var scheduleMinutes = TimeSpan.FromMinutes(int.Parse(Minutes));
        var before = timer.ScheduleStatus.Last != default
            ? timer.ScheduleStatus.Last.ToDateTimeOffset()
            : DateTimeOffset.UtcNow.Subtract(scheduleMinutes);
        var after = before.Subtract(scheduleMinutes);

        foreach (var keyword in Keywords)
        {
            var search = new Search
            {
                Keyword = keyword,
                After = after,
                Before = before
            };
            await searchCollector.AddAsync(search);
        }
    }

    [FunctionName(nameof(ExecuteSearch))]
    public async Task ExecuteSearch(
        [QueueTrigger(nameof(Search))] Search search,
        [Queue(nameof(SlackAttachment))] IAsyncCollector<SlackAttachment> attachmentCollector,
        ILogger log)
    {
        SlackAttachment CreateAttachment(RedditPost post)
            => new()
            {
                Color = SlackClient.GetColor(
                    root: post.IsSubmission,
                    knownUser: KnownUsers.Contains(post.Author, StringComparer.InvariantCultureIgnoreCase)),
                AuthorName = post.Title,
                AuthorSubname = $"u/{post.Author}",
                AuthorIcon = post.AuthorIconUrl,
                AuthorLink = $"https://reddit.com{post.Permalink}?context=8&depth=9",
                ImageUrl = post.UrlOverriddenByDest,
                Text = Markdown.ToPlainText(post.Selftext ?? post.Body).VisualizeProducts(),
                Footer = $"Posted on *<https://reddit.com/{post.SubredditNamePrefixed}|{post.SubredditNamePrefixed}>*",
                FooterIcon = "https://raw.githubusercontent.com/matkoch/mentions-tracker/main/images/reddit.png",
                Ts = post.CreatedUtc
            };

        var posts = await RedditClient.Search(
            new[] { search.Keyword },
            Subreddits,
            search.After.ToUnixTimeSeconds(),
            search.Before.ToUnixTimeSeconds());

        foreach (var post in posts)
        {
            var attachment = CreateAttachment(post);
            await attachmentCollector.AddAsync(attachment);
        }
    }

    [FunctionName(nameof(PostSlack))]
    public async Task PostSlack([QueueTrigger(nameof(SlackAttachment))] SlackAttachment attachment)
    {
        await SlackClient.Post(attachment, SlackWebhook);
    }
}

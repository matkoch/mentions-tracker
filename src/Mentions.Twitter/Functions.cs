using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Mentions.Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Tweetinvi;
using Tweetinvi.Models.V2;
using Tweetinvi.Parameters.V2;

namespace Mentions.Twitter;

public class State
{
    public int LastSearch;
    public Dictionary<int, string> SearchSinceIds = new();
}

public class Functions
{
    private readonly Configuration _configuration;
    private readonly TwitterClient _twitterClient;
    private readonly TranslationClient _translationClient;

    public Functions(
        Configuration configuration,
        TwitterClient twitterClient,
        TranslationClient translationClient)
    {
        _configuration = configuration;
        _twitterClient = twitterClient;
        _translationClient = translationClient;
    }

    public const string Minutes = "5";

    [FunctionName(nameof(ExecuteSearch))]
    public async Task ExecuteSearch(
        [TimerTrigger($"0 */{Minutes} * * * *")] TimerInfo timer,
        // [Queue(nameof(SlackAttachment))] IAsyncCollector<SlackAttachment> attachmentCollector,
        [Queue(nameof(State))] QueueClient queueClient,
        ILogger log)
    {
        var state = await GetState(queueClient);

        var searchIndex = (state.LastSearch + 1) % _configuration.Searches.Length;
        var search = _configuration.Searches.ElementAtOrDefault(searchIndex);
        if (search == null)
            return;

        var sinceId = state.SearchSinceIds.GetValueOrDefault(search.Id);

        var searchParameters = new SearchTweetsV2Parameters(string.Empty)
        {
            Query = ("(" +
                     search.Keywords.Select(x => $"\"{x}\"").Join(" OR ") +
                     ") " +
                     search.Exclusions.Select(x => $"-\"{x}\"").Join(" ")).Trim(),
            StartTime = sinceId != null ? null : DateTime.UtcNow.Date, // HH:mm:ss
            SinceId = sinceId,
            // NOTE: this seems to be an issue
            // EndTime = before.UtcDateTime
        };
        log.LogWarning("Performing search #{Index} with '{Keywords}' after {StartTime} / {SinceId}",
            searchIndex,
            searchParameters.Query,
            searchParameters.StartTime,
            searchParameters.SinceId);
        var tweetSearch = await _twitterClient.SearchV2.SearchTweetsAsync(searchParameters);

        var tweets = tweetSearch.Tweets.OrderBy(x => x.Id).ToList();
        var users = tweetSearch.Includes?.Users;
        var media = tweetSearch.Includes?.Media;

        await SaveState(queueClient, state, searchIndex, search, newSinceId: tweets.LastOrDefault()?.Id ?? sinceId);

        if (tweets.Count == 0)
            return;

        UserV2 GetUser(string id) => users.NotNull().Single(x => x.Id == id);
        MediaV2 GetMedia(string mediaKey) => media.NotNull().Single(x => x.MediaKey == mediaKey);
        string GetUserLink(string username) => $"*<https://twitter.com/{username}|@{username}>*";

        async Task<string> GetText(TweetV2 tweet)
        {
            if (tweet.PossiblySensitive)
                return ":pornhub:";

            var index = 0;
            var mentions = tweet.Entities?.Mentions?.OrderBy(x => x.Start).ToArray() ?? Array.Empty<UserMentionV2>();
            foreach (var mention in mentions)
            {
                if (index == mention.Start)
                    index = mention.End + 1;
            }

            var trimmedText = tweet.Text.Substring(index);
            var translatedText = await _translationClient.Translate(trimmedText);

            var quotedId = tweet.ReferencedTweets?.SingleOrDefault(x => x.Type == "quoted")?.Id;
            var quotedText = tweetSearch.Includes?.Tweets?.SingleOrDefault(x => x.Id == quotedId)?.Text.MarkdownQuote();
            return (translatedText + Environment.NewLine + quotedText).Trim();
        }

        async Task<SlackAttachment> CreateAttachment(TweetV2 tweet)
        {
            var user = GetUser(tweet.AuthorId);

            return new SlackAttachment
            {
                Color = SlackClient.GetColor(
                    root: tweet.InReplyToUserId == null,
                    knownUser: _configuration.KnownUsers.Contains(user.Username, StringComparer.OrdinalIgnoreCase)),
                AuthorName = user.Name,
                AuthorSubname = $"@{user.Username}",
                AuthorIcon = user.ProfileImageUrl,
                AuthorLink = $"https://twitter.com/{user.Username}/status/{tweet.Id}",
                ImageUrl = !tweet.PossiblySensitive
                    ? tweet.Attachments?.MediaKeys.Select(GetMedia).FirstOrDefault()?.Url
                    : null,
                Text = (await GetText(tweet)).VisualizeProducts(),
                Footer = tweet.InReplyToUserId != null
                    ? $"Replied to {tweet.Entities.Mentions.Select(x => GetUserLink(x.Username)).Join(", ")}"
                    : "Started conversation",
                FooterIcon = "https://raw.githubusercontent.com/matkoch/mentions-tracker/main/images/twitter.png",
                Ts = tweet.CreatedAt.ToUnixTimeSeconds()
            };
        }

        var slackClient = new SlackClient(search.SlackWebhook);
        foreach (var tweet in tweets)
        {
            if (tweet.ReferencedTweets?.Any(x => x.Type == "retweeted") ?? false)
                continue;

            var attachment = await CreateAttachment(tweet);
            await slackClient.Post(attachment);
            // await attachmentCollector.AddAsync(attachment);
        }
    }

    private async Task SaveState(
        QueueClient queueClient,
        State state,
        int searchIndex,
        Search search,
        string newSinceId)
    {
        await queueClient.ClearMessagesAsync();

        state.LastSearch = searchIndex;
        state.SearchSinceIds = _configuration.Searches
            .ToDictionary(
                x => x.Id,
                x => x.Id == search.Id
                    ? newSinceId
                    : state.SearchSinceIds.GetValueOrDefault(x.Id));

        await queueClient.SendMessageAsync(JsonConvert.SerializeObject(state));
    }

    private static async Task<State> GetState(QueueClient queueClient)
    {
        var stateJson = (await queueClient.PeekMessagesAsync()).Value.FirstOrDefault()?.MessageText;
        var state = stateJson != null ? JsonConvert.DeserializeObject<State>(stateJson) : new State();
        return state;
    }

    // [FunctionName(nameof(PostSlack))]
    // public async Task PostSlack([QueueTrigger(nameof(SlackAttachment))] SlackAttachment attachment)
    // {
    //     await _slackClient.Post(attachment);
    // }
}

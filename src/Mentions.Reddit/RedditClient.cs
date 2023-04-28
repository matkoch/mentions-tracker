using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;

namespace Mentions.Reddit;

public static class RedditClient
{
    public const string ApiUrl = "https://api.pushshift.io/reddit";

    private const string TitleUnavailable = "[Title Unavailable]";

    public static async Task<IEnumerable<RedditPost>> Search(
        string[] keywords,
        string[] subreddits,
        long after,
        long before)
    {
        async Task<RedditPost[]> GetPosts(string kind)
            => (await ApiUrl.AppendPathSegments(kind, "search")
                .SetQueryParam("q", string.Join("%20", keywords))
                .SetQueryParam("subreddit", string.Join(",", subreddits))
                .SetQueryParam("after", after)
                .SetQueryParam("before", before)
                .GetJsonAsync<RedditResponse>()).Data;

        async Task<string> GetTitle(RedditPost post)
            => (await ApiUrl.AppendPathSegments("submission", "search")
                .SetQueryParam("ids", post.LinkId[3..])
                .GetJsonAsync<RedditResponse>()).Data.SingleOrDefault()?.Title;

        async Task<string> GetIconUrl(RedditPost post)
            => (await $"https://www.reddit.com/user/{post.Author}/about.json"
                .GetJsonAsync<JObject>())["data"]!["icon_img"]!.ToString().Replace("&amp;", "&");

        var submissions = await GetPosts("submission");
        var comments = await GetPosts("comment");

        foreach (var submission in submissions)
        {
            submission.AuthorIconUrl = await GetIconUrl(submission);
        }

        foreach (var comment in comments)
        {
            comment.AuthorIconUrl = await GetIconUrl(comment);
            comment.Title = await GetTitle(comment) ?? TitleUnavailable;
        }

        return submissions.Concat(comments).OrderBy(x => x.CreatedUtc);
    }
}

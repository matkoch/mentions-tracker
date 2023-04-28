using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Mentions.Reddit;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class RedditPost
{
    public string Title;
    public string SubredditNamePrefixed;
    public string Author;
    public string AuthorIconUrl;
    public int? NumComments;
    public string LinkId;
    public string UrlOverriddenByDest;
    public string Selftext;
    public string Body;
    public string Permalink;
    public long CreatedUtc;

    public bool IsSubmission => NumComments != null;
    public bool IsComment => !IsSubmission;
}

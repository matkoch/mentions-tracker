using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Mentions.Reddit;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class RedditResponse
{
    public RedditPost[] Data;
}
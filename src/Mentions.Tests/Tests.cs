using System;
using System.Threading.Tasks;
using Mentions.Common;
using Mentions.Reddit;

namespace Mentions.Tests;

public class Tests
{
    [Fact]
    public async Task RedditClientTest()
    {
        var before = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromHours(1));
        var after = before.Subtract(TimeSpan.FromHours(5));

        var posts = await RedditClient.Search(
            new[] { "rider" },
            new[] { "csharp", "dotnet" },
            after: after.ToUnixTimeSeconds(),
            before: before.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task SlackTest()
    {
        var webhook = "";

        await SlackClient.Post(new SlackAttachment
            {
                Text = "foo",
                Color = "#F3BA00",
                FooterIcon = "https://cdn-icons-png.flaticon.com/512/1384/1384067.png",
                Footer = "text"
            },
            webhook);

        await SlackClient.Post(new SlackAttachment
            {
                Text = "foo",
                Color = "#F3BA00",
                FooterIcon = "https://www.redditstatic.com/desktop2x/img/favicon/favicon-32x32.png",
                Footer = "text"
            },
            webhook);
    }
}

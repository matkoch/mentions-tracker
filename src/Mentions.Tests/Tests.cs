using System;
using System.Threading.Tasks;
using Mentions.Common;
using Mentions.Reddit;
using Xunit.Abstractions;

namespace Mentions.Tests;

public class Tests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Test()
    {
        var client = new TranslationClient("westeurope", "");

        string text;
        text = "Das ist ziemlich cool";
        text = "のローカライズも引き続きよろしく尾根がします。";
        text = "No se que ha hecho Jetbrains pero le estoy cogiendo tirria a Rider...";
        text = "Jetbrains Rider 많이 좋아졌네.";
        text = """
            jetbrainsのriderマジでいい。
            intellij ideaとかpycharmを使ってた身からすると、お馴染みのやつ。visual studioより圧倒的に使いやすい。
            サブスク入るわ。
            """;
        text = "@kimiahdri ترکیبی که من استفاده میکنم : Resharper + GitHub Copilot در کنارشونم ChatGPT.";

        _testOutputHelper.WriteLine(await client.Translate(text));
    }

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
        var client = new SlackClient("webhook");


        await client.Post(new SlackAttachment
            {
                Text = "",
                Color = "#F3BA00",
                FooterIcon = "https://cdn-icons-png.flaticon.com/512/1384/1384067.png",
                Footer = "text"
            });
    }
}

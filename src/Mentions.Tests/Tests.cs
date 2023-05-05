using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Space.Client;
using JetBrains.Space.Common;
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
    public async Task TestSpace()
    {
        var token = "";
        var url = new Uri("https://jetbrains.team");

        var authenticationTokens = new AuthenticationTokens(token);
        var connection = new BearerTokenConnection(url, authenticationTokens);
        var client = new ChatClient(connection);

        var author = "@matkoch";
        var authorName = "Matthias 😄";
        var authorLink = "https://twitter.com/matkoch";
        var text = """
            This is my multi-line text!

            Do you like it?
            > And it also has translations inside :wow:
            """;
        var tweetLink = "https://google.com";
        var color = MessageButtonStyle.DANGER;
        var image = "https://jetbrains.team/emojis/v2/medium/wow@2?version=45";
        var footer = "Started [conversation](https://google.com)";
        var timestamp = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(10)).ToUnixTimeSeconds();

        await client.Messages.SendMessageAsync(
            recipient: MessageRecipient.Channel(ChatChannel.FromName("test-space-sdk-dotnet")),
            content: ChatMessage.Block(
                sections: new()
                {
                    MessageSectionElement.MessageSection(
                        elements: new()
                        {
                            MessageBlockElement.MessageText(
                                $"[{authorName} | {author}]({authorLink})",
                                accessory: MessageAccessoryElement.MessageIcon(
                                    new ApiIcon("twitter"),
                                    MessageStyle.PRIMARY)),
                            MessageBlockElement.MessageText(
                                text,
                                accessory: MessageAccessoryElement.MessageImage(image)),

                            MessageBlockElement.MessageInlineGroup(
                                new List<MessageInlineElement>
                                {
                                    MessageInlineElement.MessageInlineText(footer),
                                    MessageInlineElement.MessageTimestamp(timestamp, strikethrough: false)
                                },
                                textSize: MessageTextSize.SMALL),
                        })
                },
                style: MessageStyle.WARNING),
            unfurlLinks: false);
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
        var after = before.Subtract(TimeSpan.FromHours(10));

        var posts = await RedditClient.Search(
            new[] { "dotnet" },
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

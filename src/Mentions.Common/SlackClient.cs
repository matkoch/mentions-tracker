using System;
using System.Threading.Tasks;
using Flurl.Http;

namespace Mentions.Common;

public static class SlackClient
{
    public static async Task Post(SlackAttachment attachment, string webhook)
    {
        var response = await webhook
            .PostJsonAsync(new { attachments = new[] { attachment } })
            .ReceiveString();

        if (response != "ok")
            throw new Exception(response);
    }

    public const string MegaphoneUrl = "https://em-content.zobj.net/thumbs/320/twitter/322/megaphone_1f4e3.png";
    public const string SpeechBalloonUrl = "https://em-content.zobj.net/thumbs/320/twitter/322/speech-balloon_1f4ac.png";

    public static string GetIcon(bool root)
        => root ? MegaphoneUrl : SpeechBalloonUrl;

    public static string GetColor(bool root, bool knownUser)
        => (root, knownUser) switch
        {
            (_, true) => "#F3BA00",
            (true, _) => "#00ACC1",
            _ => null
        };
}

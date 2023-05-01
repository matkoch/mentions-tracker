using System;
using System.Threading.Tasks;
using Flurl.Http;

namespace Mentions.Common;

public class SlackClient
{
    private readonly string _webhook;

    public SlackClient(string webhook)
    {
        _webhook = webhook;
    }

    public async Task Post(SlackAttachment attachment)
    {
        var response = await _webhook
            .PostJsonAsync(new { attachments = new[] { attachment } })
            .ReceiveString();

        if (response != "ok")
            throw new Exception(response);
    }

    public static string GetColor(bool root, bool knownUser)
        => (root, knownUser) switch
        {
            (_, true) => "#F3BA00",
            (true, _) => "#00ACC1",
            _ => null
        };
}

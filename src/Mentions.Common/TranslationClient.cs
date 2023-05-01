using System;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Newtonsoft.Json.Linq;

namespace Mentions.Common;

public class TranslationClient
{
    private const string ApiBaseUrl = "https://api.cognitive.microsofttranslator.com";

    private readonly FlurlClient _client;

    public TranslationClient(string subscriptionRegion, string subscriptionKey)
    {
        _client = new FlurlClient(ApiBaseUrl)
            .WithHeader("Ocp-Apim-Subscription-Key", subscriptionKey)
            .WithHeader("Ocp-Apim-Subscription-Region", subscriptionRegion);
    }

    public async Task<string> Translate(string text)
    {
        var detect = await _client.Request("detect")
            .SetQueryParam("api-version", "3.0")
            .PostJsonAsync(new[] { new { Text = text } })
            .ReceiveJson<JArray>();

        if (detect[0]["language"]!.ToString() == "en")
            return null;

        var translate = await _client.Request("translate")
            .SetQueryParam("api-version", "3.0")
            .SetQueryParam("to", "en")
            .PostJsonAsync(new[] { new { Text = text } })
            .ReceiveJson<JArray>();

        var translation = translate[0]["translations"]![0]!["text"]!.ToString();

        return text + Environment.NewLine +
               translation.Split(Environment.NewLine).Select(x => $"> {x}").Join(Environment.NewLine);
    }
}

using System.IO;
using Mentions.Common;
using Mentions.Reddit;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tweetinvi;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Mentions.Reddit;

public class Configuration
{
    public string[] Keywords;
    public string[] Exclusions;
    public string[] KnownUsers;
}

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        builder.Services.AddHttpClient("Default");

        builder.Services.AddSingleton(_ =>
            new Configuration
            {
                Keywords = config[nameof(Configuration.Keywords)].Split(","),
                Exclusions = config[nameof(Configuration.Exclusions)].Split(","),
                KnownUsers = config[nameof(Configuration.KnownUsers)].Split(",")
            });

        builder.Services.AddSingleton(_ =>
            new TwitterClient(
                config["TwitterConsumerKey"],
                config["TwitterConsumerSecret"],
                config["TwitterConsumerAccessToken"],
                config["TwitterConsumerAccessTokenSecret"]));

        builder.Services.AddSingleton(_ =>
            new SlackClient(config["SlackWebhook"]));

        builder.Services.AddSingleton(_ =>
            new TranslationClient(
                config["TranslationsSubscriptionRegion"],
                config["TranslationsSubscriptionKey"]));
    }
}

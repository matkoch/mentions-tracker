using System;
using System.IO;
using System.Linq;
using System.Text;
using Mentions.Common;
using Mentions.Twitter;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Tweetinvi;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Mentions.Twitter;

public class Configuration
{
    public string[] KnownUsers;
    public Search[] Searches;
}

public class Search
{
    public int Id => Keywords.Join(",").GetHashCode();
    public string[] Keywords;
    public string[] Exclusions;
    public string SlackWebhook;
}

public class TwitterCredentials
{
    public string ConsumerKey;
    public string ConsumerSecret;
    public string AccessToken;
    public string AccessTokenSecret;
}
public class TranslationCredentials
{
    public string SubscriptionKey;
    public string SubscriptionRegion;
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

        T GetConfigEntry<T>()
        {
            var jwt = config[typeof(T).Name];
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(jwt));
            return JsonConvert.DeserializeObject<T>(json);
        }

        var configuration = GetConfigEntry<Configuration>();
        var twitterCredentials = GetConfigEntry<TwitterCredentials>();
        var translationCredentials = GetConfigEntry<TranslationCredentials>();
        builder.Services.AddSingleton(_ => configuration);

        builder.Services.AddSingleton(_ =>
            new TwitterClient(
                twitterCredentials.ConsumerKey.NotNull(),
                twitterCredentials.ConsumerSecret.NotNull(),
                twitterCredentials.AccessToken.NotNull(),
                twitterCredentials.AccessTokenSecret.NotNull()));

        builder.Services.AddSingleton(_ =>
            new TranslationClient(
                translationCredentials.SubscriptionRegion.NotNull(),
                translationCredentials.SubscriptionKey.NotNull()));
    }
}

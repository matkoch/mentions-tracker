using System.IO;
using Mentions.Common;
using Mentions.Reddit;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Mentions.Reddit;

public class Configuration
{
    public string[] Keywords;
    public string[] Subreddits;
    public string[] Exclusions;
    public string[] KnownUsers;
    public string SlackWebhook;
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
                Subreddits = config[nameof(Configuration.Subreddits)].Split(","),
                Exclusions = config[nameof(Configuration.Exclusions)].Split(","),
                KnownUsers = config[nameof(Configuration.KnownUsers)].Split(",")
            });

        builder.Services.AddSingleton(_ =>
            new SlackClient(config["SlackWebhook"]));
    }
}

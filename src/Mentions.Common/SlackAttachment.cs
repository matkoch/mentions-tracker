using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Mentions.Common;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class SlackAttachment
{
    public string Color;
    public string AuthorName;
    public string AuthorIcon;
    public string AuthorLink;
    public string AuthorSubname;
    public string Title;
    public string TitleLink;
    public string ImageUrl;
    public string Text;
    public string Footer;
    public string FooterIcon;
    public long Ts;
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Mentions.Common;

public static class Extensions
{
    public static T NotNull<T>(
        this T obj,
        string message = null,
        [CallerArgumentExpression("obj")]
        string argumentExpression = null)
        where T : class
    {
        if (obj == null)
        {
            throw new ArgumentException(
                message ?? $"Expected object of type '{typeof(T).FullName}' to be not null",
                message == null ? argumentExpression : null);
        }

        return obj;
    }

    public static string MarkdownQuote(this string str)
    {
        return str.Split(Environment.NewLine).Select(x => $"> {x}").Join(Environment.NewLine);
    }

    public static DateTimeOffset ToDateTimeOffset(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime);
    }

    public static string Join(this IEnumerable<string> values, string separator)
    {
        return string.Join(separator, values);
    }

    public static string VisualizeProducts(this string text)
        => new Dictionary<string, string>
        {
            { "jet ?brains|jet ?brians", "jetbrains" },
            { "resharper|reshaper", "resharper" },
            { "rider|ryder|raider", "rider" },
            { "c#|csharp", "csharp_file" },
            { "f#|fsharp", "fsharp" },
            { "unity", "unity" },
            { "roslyn", "roslyn" },
            { "avalonia", "avalonia" },
            { "blazor", "blazor" },
            { "aws", "aws_logo" },
            { "unreal", "unreal" },
            { "vscode|vs code|visual ?studio code", "vscode" },
            { "vs|visual ?studio", "visualstudio" },
            { "windows", "windows" },
            { "apple|mac|mac ?book|macos", "macos" },
            { "linux|ubuntu", "linux" },
            { "github", "github" },
            { "docker", "docker" },
            { "nuke|nuke build|nuke\\.build|nuke-build|nukebuild", "nuke" },
            { "cake|cake build|cake-build|cakebuild", "cake" },
        }.Aggregate(text, (x, p) => Regex.Replace(x, $@"(\s+\.?)({p.Key})(\.?\s+)", $"$1:{p.Value}:$3", RegexOptions.IgnoreCase));
}

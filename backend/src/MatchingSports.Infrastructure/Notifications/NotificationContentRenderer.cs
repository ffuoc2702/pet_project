using AngleSharp.Html.Parser;
using Ganss.Xss;
using MatchingSports.Application.Abstractions;

namespace MatchingSports.Infrastructure.Notifications;

public sealed class NotificationContentRenderer : INotificationContentRenderer
{
    public (string Html, string Text) Render(string body)
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(["p", "br", "b", "strong", "i", "em", "u", "ul", "ol", "li", "a", "blockquote", "h2", "h3"]);
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(["href", "title"]);
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(["https", "http"]);
        var html = sanitizer.Sanitize(body);
        // Read text from the parsed DOM rather than trying to remove markup using a regex.
        var document = new HtmlParser().ParseDocument(html);
        foreach (var element in document.QuerySelectorAll("br")) element.Replace(document.CreateTextNode("\n"));
        foreach (var element in document.QuerySelectorAll("p,li,h2,h3,blockquote"))
            element.AppendChild(document.CreateTextNode("\n"));
        return (html, document.Body!.TextContent.Trim());
    }
}

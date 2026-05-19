using System.Xml.Linq;

namespace Nuscope.Cli;

internal sealed class DocumentationComments
{
    private readonly IReadOnlyDictionary<string, string> _comments;

    private DocumentationComments(IReadOnlyDictionary<string, string> comments)
    {
        _comments = comments;
    }

    public static DocumentationComments Empty { get; } = new(new Dictionary<string, string>());

    /// <summary>
    /// Loads compiler-generated XML documentation comments keyed by XML documentation member ID.
    /// </summary>
    public static DocumentationComments Load(Stream stream)
    {
        var document = XDocument.Load(stream);
        var comments = document
            .Descendants("member")
            .Select(member => new
            {
                Id = member.Attribute("name")?.Value,
                Text = Normalize(member.Element("summary"))
            })
            .Where(member => !string.IsNullOrWhiteSpace(member.Id) && !string.IsNullOrWhiteSpace(member.Text))
            .ToDictionary(member => member.Id!, member => member.Text!, StringComparer.Ordinal);

        return new DocumentationComments(comments);
    }

    public string? Get(string? documentationId) =>
        documentationId is not null && _comments.TryGetValue(documentationId, out var comment) ? comment : null;

    private static string? Normalize(XElement? element)
    {
        if (element is null)
        {
            return null;
        }

        var value = RenderNodes(element.Nodes());
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string RenderNodes(IEnumerable<XNode> nodes) =>
        string.Concat(nodes.Select(RenderNode));

    private static string RenderNode(XNode node) =>
        node switch
        {
            XText text => text.Value,
            XElement element => RenderElement(element),
            _ => string.Empty
        };

    private static string RenderElement(XElement element) =>
        element.Name.LocalName switch
        {
            "see" => RenderSee(element),
            "seealso" => RenderSee(element),
            "paramref" => element.Attribute("name")?.Value ?? string.Empty,
            "typeparamref" => element.Attribute("name")?.Value ?? string.Empty,
            "c" => element.Value,
            "code" => element.Value,
            "para" => $" {RenderNodes(element.Nodes())} ",
            _ => RenderNodes(element.Nodes())
        };

    private static string RenderSee(XElement element)
    {
        var langword = element.Attribute("langword")?.Value;
        if (!string.IsNullOrWhiteSpace(langword))
        {
            return langword;
        }

        var cref = element.Attribute("cref")?.Value;
        if (!string.IsNullOrWhiteSpace(cref))
        {
            return FormatCref(cref);
        }

        var href = element.Attribute("href")?.Value;
        if (!string.IsNullOrWhiteSpace(href))
        {
            return href;
        }

        return RenderNodes(element.Nodes());
    }

    private static string FormatCref(string cref)
    {
        var separatorIndex = cref.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex >= 0 ? cref[(separatorIndex + 1)..] : cref;
    }
}

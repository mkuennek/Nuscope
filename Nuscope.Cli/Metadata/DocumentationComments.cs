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
                Text = Normalize(member.Element("summary")?.Value)
            })
            .Where(member => !string.IsNullOrWhiteSpace(member.Id) && !string.IsNullOrWhiteSpace(member.Text))
            .ToDictionary(member => member.Id!, member => member.Text!, StringComparer.Ordinal);

        return new DocumentationComments(comments);
    }

    public string? Get(string? documentationId) =>
        documentationId is not null && _comments.TryGetValue(documentationId, out var comment) ? comment : null;

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}

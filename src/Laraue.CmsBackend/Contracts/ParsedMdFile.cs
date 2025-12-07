namespace Laraue.CmsBackend.Contracts;

public sealed class ParsedMdFile
{
    public required string ContentType { get; init; }
    public required string Content { get; init; }
    public required ICollection<ParsedMdFileProperty> Properties { get; init; }
    public required ICollection<ArticleInnerLink> InnerLinks { get; init; }
    public required FilePath Path { get; init; }
}

public sealed record ParsedMdFileProperty
{
    public required string Name { get; init; }
    public required object? Value { get; init; }
    public int SourceLineNumber { get; init; }
}

public sealed record ArticleInnerLink
{
    public required int Level { get; init; }
    public required string Title { get; init; }
    public required string Link { get; init; }
}
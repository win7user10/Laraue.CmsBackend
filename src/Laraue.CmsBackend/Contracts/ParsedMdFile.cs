namespace Laraue.CmsBackend.Contracts;

public sealed record ParsedMdFile
{
    public required string Id { get; init; }
    public required string ContentType { get; init; }
    public required string Content { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public required ICollection<ParsedMdFileProperty> Properties { get; init; }
    public required ICollection<ArticleInnerLink> InnerLinks { get; init; }
    public required string Path { get; init; }
}

public sealed record ParsedMdFileProperty
{
    public required string Name { get; init; }
    public required string Value { get; init; }
    public int SourceLineNumber { get; init; }
}

public sealed record ArticleInnerLink
{
    public required int Level { get; init; }
    public required string Title { get; init; }
    public required string Link { get; init; }
}
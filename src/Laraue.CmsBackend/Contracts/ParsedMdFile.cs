namespace Laraue.CmsBackend.Contracts;

public sealed class ParsedMdFile
{
    /// <summary>
    /// File name without extension, 'index'.
    /// </summary>
    public required string? FileName { get; init; }
    public required string ContentType { get; init; }
    public required string Content { get; init; }
    public required ICollection<ParsedMdFileProperty> Properties { get; init; }
    public required ICollection<ArticleInnerLink> InnerLinks { get; init; }
    
    /// <summary>
    /// Physical path, 'articles/index.md'.
    /// </summary>
    public required FilePath PhysicalPath { get; init; }
    
    /// <summary>
    /// The path that matches the specified rules, 'articles'.
    /// </summary>
    public required FilePath LogicalPath { get; init; }
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
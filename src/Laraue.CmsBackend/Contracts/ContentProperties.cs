namespace Laraue.CmsBackend.Contracts;

public record ContentProperties(
    string Markdown,
    FilePath Path,
    string Id, 
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
}
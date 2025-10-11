namespace Laraue.CmsBackend.Contracts;

public record ContentProperties(
    string Markdown,
    string Path,
    string Id, 
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
}
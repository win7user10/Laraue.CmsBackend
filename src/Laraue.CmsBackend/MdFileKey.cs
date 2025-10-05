namespace Laraue.CmsBackend;

public record MdFileKey
{
    public required string ContentType { get; init; }
    public required string Id { get; init; }
}
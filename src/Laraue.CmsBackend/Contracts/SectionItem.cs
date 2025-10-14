namespace Laraue.CmsBackend.Contracts;

public class SectionItem
{
    public required string FileName { get; init; }
    public required string[] RelativePath { get; init; }
    public required string[] FullPath { get; init; }
    public required SectionItem[] Children { get; init; }
    public required bool HasContent { get; init; }
    public required string? Title { get; init; }
}
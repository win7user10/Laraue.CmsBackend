namespace Laraue.CmsBackend.Contracts;

public class SectionItem
{
    public required string FileName { get; init; }
    public required string[] RelativePath { get; init; }
    public required string[] FullPath { get; init; }
    public required SectionItem[] Children { get; set; }
    public required bool HasContent { get; init; }
    public required string? Title { get; init; }
    public required ProcessedMdFile? MdFile { get; init; }

    public IEnumerable<SectionItem> GetAllChildren()
    {
        return GetAllChildren(Children);
    }
    
    private static IEnumerable<SectionItem> GetAllChildren(SectionItem[] items)
    {
        foreach (var x in items)
        {
            yield return x;
            foreach (var y in GetAllChildren(x.Children))
                yield return y;
        }
    }
}
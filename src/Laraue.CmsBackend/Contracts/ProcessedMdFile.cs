namespace Laraue.CmsBackend.Contracts;

public sealed class ProcessedMdFile : Dictionary<string, object>
{
    public ProcessedMdFile(ParsedMdFile mdFile)
        : base(new Dictionary<string, object>
        {
            ["contentType"] = mdFile.ContentType,
            ["fileName"] = mdFile.FileName,
            ["updatedAt"] = mdFile.UpdatedAt,
            ["createdAt"] = mdFile.CreatedAt,
            ["content"] = mdFile.Content,
            ["path"] = mdFile.Path.Segments,
            ["innerLinks"] = mdFile.InnerLinks,
        })
    {}
    
    public ProcessedMdFile(Dictionary<string, object> source)
        : base(source)
    {}
}
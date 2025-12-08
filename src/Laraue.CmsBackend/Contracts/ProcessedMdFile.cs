namespace Laraue.CmsBackend.Contracts;

public sealed class ProcessedMdFile : Dictionary<string, object>
{
    public ProcessedMdFile(ParsedMdFile mdFile)
        : base(MapParsedFile(mdFile))
    {}
    
    public ProcessedMdFile(Dictionary<string, object> source)
        : base(source)
    {}

    private static Dictionary<string, object> MapParsedFile(ParsedMdFile mdFile)
    {
        var result = new Dictionary<string, object>
        {
            ["contentType"] = mdFile.ContentType,
            ["content"] = mdFile.Content,
            ["path"] = mdFile.LogicalPath.Segments,
            ["innerLinks"] = mdFile.InnerLinks,
        };

        if (mdFile.FileName is not null)
        {
            result["fileName"] = mdFile.FileName;
        }
        
        return result;
    }
}
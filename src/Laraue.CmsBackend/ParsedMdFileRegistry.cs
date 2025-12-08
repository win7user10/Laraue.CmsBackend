using Laraue.CmsBackend.Contracts;

namespace Laraue.CmsBackend;

public class ParsedMdFileRegistry
{
    private readonly Dictionary<FilePath, ParsedMdFile> _parsedMdFiles = new ();
    
    public ParsedMdFileRegistry Add(ParsedMdFile parsedMdFile)
    {
        if (!_parsedMdFiles.TryAdd(parsedMdFile.LogicalPath, parsedMdFile))
        {
            throw new InvalidOperationException($"Content '{parsedMdFile.ContentType}' path: '{parsedMdFile.LogicalPath}' has already been added.");
        }
        
        return this;
    }
    
    public ICollection<ParsedMdFile> Values => _parsedMdFiles.Values;
}
using Laraue.CmsBackend.Contracts;

namespace Laraue.CmsBackend;

public class ParsedMdFileRegistry
{
    private readonly Dictionary<FilePath, ParsedMdFile> _parsedMdFiles = new ();
    
    public ParsedMdFileRegistry Add(ParsedMdFile parsedMdFile)
    {
        if (!_parsedMdFiles.TryAdd(parsedMdFile.Path, parsedMdFile))
        {
            throw new InvalidOperationException($"Content '{parsedMdFile.ContentType}' path: '{parsedMdFile.Path}' has already been added.");
        }
        
        return this;
    }
    
    public ICollection<ParsedMdFile> Values => _parsedMdFiles.Values;
}
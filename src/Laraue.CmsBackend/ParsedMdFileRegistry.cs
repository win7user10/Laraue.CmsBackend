using Laraue.CmsBackend.Contracts;

namespace Laraue.CmsBackend;

public class ParsedMdFileRegistry
{
    private readonly Dictionary<FilePath, ParsedMdFile> _parsedMdFiles = new ();
    
    public ParsedMdFileRegistry Add(ParsedMdFile parsedMdFile)
    {
        var path = parsedMdFile.Path.Segments.Append(parsedMdFile.FileName);
        if (!_parsedMdFiles.TryAdd(new FilePath(path.ToArray()), parsedMdFile))
        {
            throw new InvalidOperationException($"Content '{parsedMdFile.ContentType}':'{parsedMdFile.FileName}' has already been added");
        }
        
        return this;
    }
    
    public ICollection<ParsedMdFile> Values => _parsedMdFiles.Values;
}
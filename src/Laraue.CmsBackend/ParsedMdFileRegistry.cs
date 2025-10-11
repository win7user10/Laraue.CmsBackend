using Laraue.CmsBackend.Contracts;

namespace Laraue.CmsBackend;

public class ParsedMdFileRegistry
{
    private readonly Dictionary<MdFileKey, ParsedMdFile> _parsedMdFiles = new ();
    
    public ParsedMdFileRegistry Add(ParsedMdFile parsedMdFile)
    {
        if (!_parsedMdFiles.TryAdd(new MdFileKey { Id = parsedMdFile.Id, ContentType = parsedMdFile.ContentType}, parsedMdFile))
        {
            throw new InvalidOperationException($"Content '{parsedMdFile.ContentType}':'{parsedMdFile.Id}' has already been added");
        }
        
        return this;
    }
    
    public ICollection<ParsedMdFile> Values => _parsedMdFiles.Values;
}
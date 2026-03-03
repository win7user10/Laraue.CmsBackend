using Laraue.CmsBackend.Contracts;

namespace Laraue.CmsBackend;

public class ParsedMdFileRegistry
{
    private readonly Dictionary<FilePath, ParsedMdFileByLanguageCode> _parsedMdFiles = new ();
    
    public ParsedMdFileRegistry Add(ParsedMdFile parsedMdFile)
    {
        if (!_parsedMdFiles.TryGetValue(parsedMdFile.LogicalPath, out var filesDictionary))
        {
            _parsedMdFiles[parsedMdFile.LogicalPath] = new ParsedMdFileByLanguageCode
            {
                [parsedMdFile.LanguageCode] = parsedMdFile,
            };
        }
        else
        {
            if (!filesDictionary.TryAdd(parsedMdFile.LanguageCode, parsedMdFile))
            {
                throw new InvalidOperationException(
                    $"Content '{parsedMdFile.ContentType}' " +
                    $"path: '{parsedMdFile.LogicalPath}' " +
                    $"lang: '{parsedMdFile.LanguageCode}' " +
                    $"has already been added.");
            }
        }
        
        return this;
    }
    
    public ICollection<ParsedMdFile> Values => _parsedMdFiles.Values
        .SelectMany(x => x.Values)
        .ToArray();
}
    
public class ParsedMdFileByLanguageCode : Dictionary<string, ParsedMdFile>
{
}
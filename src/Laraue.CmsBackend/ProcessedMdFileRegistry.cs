using System.Diagnostics.CodeAnalysis;

namespace Laraue.CmsBackend;

public class ProcessedMdFileRegistry
{
    private readonly Dictionary<MdFileKey, ProcessedMdFile> _processedMdFiles =  new ();

    public bool TryAdd(ProcessedMdFile mdFile)
    {
        return _processedMdFiles.TryAdd(
            new MdFileKey
            {
                Id = (string)mdFile["id"],
                ContentType = (string)mdFile["contentType"]
            },
            mdFile);
    }

    public bool TryGet(MdFileKey key, [NotNullWhen(true)] out ProcessedMdFile? mdFile)
    {
        return _processedMdFiles.TryGetValue(key, out mdFile);
    }

    public IEnumerable<ProcessedMdFile> GetEntities()
    {
        return _processedMdFiles.Values;
    }
}
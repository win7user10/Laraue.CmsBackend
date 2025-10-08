using System.Diagnostics.CodeAnalysis;

namespace Laraue.CmsBackend;

public class ContentTypeRegistry
{
    private readonly Dictionary<string, DocumentType> _contentTypes = new ();
    
    public ContentTypeRegistry AddContentType<TContentType>() where TContentType : DocumentType
    {
        var contentType = Activator.CreateInstance<TContentType>();

        if (!_contentTypes.TryAdd(contentType.Id, contentType))
        {
            throw new InvalidOperationException($"ContentType {contentType.Id} has already been added");
        }
        
        return this;
    }

    public bool TryGetContentType(string id, [NotNullWhen(true)] out DocumentType? contentType)
    {
        return _contentTypes.TryGetValue(id, out contentType);
    } 
}
using System.Diagnostics.CodeAnalysis;

namespace Laraue.CmsBackend;

public class ContentTypeRegistry
{
    private readonly Dictionary<string, ContentType> _contentTypes = new ();
    
    public ContentTypeRegistry AddContentType<TContentType>() where TContentType : ContentType
    {
        var contentType = Activator.CreateInstance<TContentType>();

        if (!_contentTypes.TryAdd(contentType.Id, contentType))
        {
            throw new InvalidOperationException($"ContentType {contentType.Id} has already been added");
        }
        
        return this;
    }

    public bool TryGetContentType(string id, [NotNullWhen(true)] out ContentType? contentType)
    {
        return _contentTypes.TryGetValue(id, out contentType);
    } 
}
namespace Laraue.CmsBackend;

public abstract class BaseDocumentType
{
    /// <summary>
    /// Document title (section 'title' of the md file). Add cause a lot of api requires this property.
    /// </summary>
    public string? Title { get; init; }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class DocumentTypeAttribute : Attribute
{
    public string ContentType { get; }

    public DocumentTypeAttribute(string contentType)
    {
        ContentType = contentType;
    }
}
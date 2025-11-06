namespace Laraue.CmsBackend;

public abstract class BaseContentType
{
    /// <summary>
    /// Document title (section 'title' of the md file). Add cause a lot of api requires this property.
    /// </summary>
    public string? Title { get; init; }
    
    /// <summary>
    /// The file created attribute. 
    /// </summary>
    public DateTime? CreatedAt { get; init; }
    
    /// <summary>
    /// The file modified attribute. When filled, sitemap generator can write modification date.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ContentTypeAttribute : Attribute
{
    public string ContentType { get; }

    public ContentTypeAttribute(string contentType)
    {
        ContentType = contentType;
    }
}
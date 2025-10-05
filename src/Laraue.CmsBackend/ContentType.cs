namespace Laraue.CmsBackend;

public abstract class ContentType
{
    public abstract string Id { get; }
    public abstract ContentTypeProperty[] Properties { get; }
}

public class ContentTypeProperty
{
    public required string Name { get; set; }
    public ContentTypePropertyType Type { get; set; }
    public bool Required { get; set; }
}

public enum ContentTypePropertyType
{
    String,
    Number,
    DateTime,
    Float,
}
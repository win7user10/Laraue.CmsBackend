namespace Laraue.CmsBackend;

public abstract class DocumentType
{
    public abstract string Id { get; }
    public abstract DocumentTypeProperty[] Properties { get; }
}

public abstract class DocumentTypeProperty
{
    public required string Name { get; set; }
    public ContentTypePropertyType Type { get; set; }
    public bool Required { get; set; }
}

public class ArrayTypeProperty : DocumentTypeProperty
{
}

public class ScalarTypeProperty : DocumentTypeProperty
{
}

public enum ContentTypePropertyType
{
    String,
    Number,
    DateTime,
    Float,
}
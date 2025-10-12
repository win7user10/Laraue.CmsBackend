namespace Laraue.CmsBackend;

// todo - IF DOC CAN BE WITHOUT TYPE, default implementation should be used? What is default impl? It should have title?
public class DocumentType(string id, DocumentTypeProperty[] properties)
{
    public string Id { get; } = id;
    public DocumentTypeProperty[] Properties { get; } = properties;
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
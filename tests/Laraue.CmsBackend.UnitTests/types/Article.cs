namespace Laraue.CmsBackend.UnitTests.types;

public class Article : DocumentType
{
    public override string Id => "article";

    public override DocumentTypeProperty[] Properties =>
    [
        new ScalarTypeProperty { Name = "name", Type = ContentTypePropertyType.String, Required = true },
        new ArrayTypeProperty { Name = "tags", Type = ContentTypePropertyType.String, Required = true },
    ];
}
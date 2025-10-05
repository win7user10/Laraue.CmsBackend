namespace Laraue.CmsBackend.UnitTests.types;

public class Article : ContentType
{
    public override string Id => "article";

    public override ContentTypeProperty[] Properties =>
    [
        new () { Name = "name", Type = ContentTypePropertyType.String, Required = true }
    ];
}
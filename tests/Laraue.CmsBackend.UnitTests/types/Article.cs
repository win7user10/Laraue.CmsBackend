namespace Laraue.CmsBackend.UnitTests.types;

public class Article : BaseContentType
{
    public required string Name { get; set; }
    public required string[] Tags { get; set; }
}
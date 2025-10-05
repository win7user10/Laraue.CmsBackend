using Laraue.Core.Exceptions.Web;

namespace Laraue.CmsBackend.UnitTests;

public class CmsBackendTests
{
    private readonly ICmsBackend _cmsBackend;
    
    public CmsBackendTests()
    {
        var files = new ProcessedMdFileRegistry();

        files.TryAdd(new ProcessedMdFile
        {
            ContentType = "article",
            Content = "hi",
            Id = "introduction",
            UpdatedAt = new DateTime(2020, 01, 01),
            Properties =
            [
                new ProcessedMdFileProperty()
                {
                    Name = "project",
                    Value = "project1",
                }
            ]
        });
        
        _cmsBackend = new CmsBackend(files);
    }
    
    [Fact]
    public void GetEntity_ShouldThrows_WhenEntityIsNotExists()
    {
        Assert.Throws<NotFoundException>(() => _cmsBackend.GetEntity(new GetEntityRequest
        {
            Key = new MdFileKey { Id = "1", ContentType = "1" }
        }));
    }
    
    [Fact]
    public void GetEntity_ShouldReturnsOnlyRequestedField_WhenRequestHasListOfRequiredProperties()
    {
        var result = _cmsBackend.GetEntity(new GetEntityRequest
        {
            Key = new MdFileKey { Id = "introduction", ContentType = "article" },
            Properties = ["project"]
        });
        
        var property = Assert.Single(result);
        Assert.Equal("project1", property.Value);
        Assert.Equal("project", property.Key);
    }
}
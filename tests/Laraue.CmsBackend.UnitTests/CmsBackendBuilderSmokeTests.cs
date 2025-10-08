using Laraue.CmsBackend.Extensions;
using Laraue.CmsBackend.UnitTests.types;

namespace Laraue.CmsBackend.UnitTests;

public class CmsBackendBuilderSmokeTests
{
    [Fact]
    public void Smoke_ShouldBePassed_Always()
    {
        var cmsBackend = new CmsBackendBuilder(new MarkdownParser(), new MarkdownProcessor())
            .AddContentType<Article>()
            .AddContentFolder("articles")
            .Build();

        var article = cmsBackend.GetEntity(new GetEntityRequest
        {
            Key = new MdFileKey { Id = "introduction", ContentType = "article" }
        });
        
        Assert.NotNull(article);
        
        var tags = Assert.IsType<object[]>(article["tags"]);
        Assert.Equal(2, tags.Length);
    }
}
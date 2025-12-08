using Laraue.CmsBackend.Extensions;
using Laraue.CmsBackend.MarkdownTransformation;
using Laraue.CmsBackend.UnitTests.types;

namespace Laraue.CmsBackend.UnitTests;

public class CmsBackendBuilderTests
{
    private readonly ICmsBackend _cmsBackend;
    
    public CmsBackendBuilderTests()
    {
        _cmsBackend = new CmsBackendBuilder(
                new MarkdownParser(
                    new MarkdownToHtmlTransformer(),
                    new ArticleInnerLinksGenerator()),
                new MarkdownProcessor())
            .AddContentType<Article>()
            .AddContentFolder("articles")
            .Build();
    }
    
    [Fact]
    public void GetEntity_ShouldWorkWithNamedFile_Always()
    {
        var article = _cmsBackend.GetEntity(new GetEntityRequest
        {
            Path = ["articles", "introduction"]
        });
        
        Assert.NotNull(article);
        
        var tags = Assert.IsType<object[]>(article["tags"]);
        var fileName = Assert.IsType<string>(article["fileName"]);
        var path = Assert.IsType<string[]>(article["path"]);
        
        Assert.Equal(2, tags.Length);
        Assert.Equal("introduction", fileName);
        Assert.Equal(["articles", "introduction"], path);
    }
    
    [Fact]
    public void GetEntity_ShouldWorkWithIndexFile_Always()
    {
        var section = _cmsBackend.GetEntity(new GetEntityRequest
        {
            Path = ["articles"]
        });
        
        Assert.NotNull(section);
        
        var title = Assert.IsType<string>(section["title"]);
        var path = Assert.IsType<string[]>(section["path"]);
        
        Assert.DoesNotContain("fileName", section);
        Assert.Equal("Index page", title);
        Assert.Equal(["articles"], path);
    }
}
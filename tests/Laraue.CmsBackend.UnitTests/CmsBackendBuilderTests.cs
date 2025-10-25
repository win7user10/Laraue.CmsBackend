using Laraue.CmsBackend.Extensions;
using Laraue.CmsBackend.MarkdownTransformation;
using Laraue.CmsBackend.UnitTests.types;
using Laraue.Core.DataAccess.Contracts;

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
        Assert.Equal(2, tags.Length);
    }
    
    [Fact]
    public void GetEntity_ShouldWorkWithIndexFile_Always()
    {
        var section = _cmsBackend.GetEntity(new GetEntityRequest
        {
            Path =  ["articles"]
        });
        
        Assert.NotNull(section);
        
        var title = Assert.IsType<string>(section["title"]);
        Assert.Equal("Index page", title);
    }
}
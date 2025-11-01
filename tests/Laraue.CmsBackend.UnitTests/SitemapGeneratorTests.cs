using Laraue.CmsBackend.Contracts;
using Laraue.CmsBackend.MarkdownTransformation;

namespace Laraue.CmsBackend.UnitTests;

public class SitemapGeneratorTests
{
    private readonly ISitemapGenerator _sitemapGenerator;
    
    public SitemapGeneratorTests()
    {
        var content1Text = "hi";
        var content1 =  new ContentProperties(
            content1Text,
            new FilePath(["docs", "articles"]),
            "article1",
            new DateTime(2020, 01, 01),
            new DateTime(2020, 01, 01));
        
        var cmsBackend = new CmsBackendBuilder(
                new MarkdownParser(
                    new MarkdownToHtmlTransformer(),
                    new ArticleInnerLinksGenerator()),
                new MarkdownProcessor())
            .AddContent(content1)
            .Build();
        
        _sitemapGenerator = new SitemapGenerator(cmsBackend);
    }

    [Fact]
    public void SitemapItems_ShouldBeGenerated_Always()
    {
        var items = _sitemapGenerator.GetItems();
        var item = Assert.Single(items);
        
        Assert.Equal("docs/articles/article1", item.Loc);
        Assert.Equal(new DateTime(2020, 01, 01), item.LastMod);
    }
}
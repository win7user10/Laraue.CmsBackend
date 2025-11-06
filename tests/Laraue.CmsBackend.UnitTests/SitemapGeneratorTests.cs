using Laraue.CmsBackend.Contracts;
using Laraue.CmsBackend.MarkdownTransformation;

namespace Laraue.CmsBackend.UnitTests;

public class SitemapGeneratorTests
{
    private readonly ISitemapGenerator _sitemapGenerator;
    
    public SitemapGeneratorTests()
    {
        var content1Text = @"
---
createdAt: 2020-01-01
updatedAt: 2020-01-01
---
hi";
        var content1 =  new ContentProperties(
            content1Text,
            new FilePath(["docs", "articles"]),
            "article1");
        
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
        
        Assert.Equal("docs/articles/article1", item.Location);
        Assert.Equal(new DateTime(2020, 01, 01), item.LastModified);
    }
    
    [Fact]
    public void Sitemap_ShouldBeGenerated_Always()
    {
        var sitemapItem = new SitemapItemDto("location1", new DateTime(2020, 01, 01));

        var sitemapXml = _sitemapGenerator.GenerateSitemap(
            new GenerateSitemapRequest { BaseAddress = "http://test.com/" },
            [sitemapItem]);
        
        Assert.Equal("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"><url><loc>http://test.com/location1</loc><lastmod>2020-01-01T00:00:00Z</lastmod></url></urlset>", sitemapXml);
    }
}
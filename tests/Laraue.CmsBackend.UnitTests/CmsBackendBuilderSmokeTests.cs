using Laraue.CmsBackend.UnitTests.types;

namespace Laraue.CmsBackend.UnitTests;

public class CmsBackendBuilderSmokeTests
{
    [Fact]
    public void Smoke_ShouldBePassed_Always()
    {
        var testMarkdown = @"---
id: introduction
type: article
name: The new library presentation
---

## How to use that?

The library is easy to use in CSharp with or without database.
";
        
        var cmsBackend = new CmsBackendBuilder(new MarkdownParser(), new MarkdownProcessor())
            .AddContentType<Article>()
            .AddContent(testMarkdown, new DateTime(2020, 01, 01))
            .Build();

        var article = cmsBackend.GetEntity(new GetEntityRequest
        {
            Key = new MdFileKey { Id = "introduction", ContentType = "article" }
        });
        
        Assert.NotNull(article);
    }
}
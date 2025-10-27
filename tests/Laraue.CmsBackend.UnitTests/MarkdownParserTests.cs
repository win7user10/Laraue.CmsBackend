using Laraue.CmsBackend.MarkdownTransformation;
using Laraue.Interpreter.Parsing.Extensions;
using Laraue.Interpreter.Scanning.Extensions;

namespace Laraue.CmsBackend.UnitTests;

public class MarkdownParserTests
{
    [Fact]
    public void Settings_ShouldNotBeInOutput_Always()
    {
        var contentText = @"---
tags: [tag1, tag2]
project: project1
type: unitTestArticle
---
hi";

        Assert.Equal("<p>hi</p>", ToHtml(contentText));
    }
    
    [Fact]
    public void MarkdownWithoutSettings_ShouldBeRendered_Always()
    {
        var contentText = "hi";

        Assert.Equal("<p>hi</p>", ToHtml(contentText));
    }
    
    [Fact]
    public void Tables_ShouldBeRendered_Always()
    {
        var contentText = @"| Name | Age |
| --- | --- |
| Henry | 15 |
| Alex | 17 |";

        Assert.Equal("<table><thead><tr><th>Name</th><th>Age</th></tr></thead><tbody><tr><td>Henry</td><td>15</td></tr><tr><td>Alex</td><td>17</td></tr></tbody></table>", ToHtml(contentText));
    }
    
    [Fact]
    public void OrderedLists_ShouldBeRendered_Always()
    {
        var contentText = @"1. Item #1
1. Item #2
    1. Item #3";

        Assert.Equal("<ol><li>Item #1</li><li>Item #2</li><ol><li>Item #3</li></ol></ol>", ToHtml(contentText));
    }
    
    [Fact]
    public void UnorderedLists_ShouldBeRendered_Always()
    {
        var contentText = @"- Item #1
- Item #2
    - Item #3";

        Assert.Equal("<ul><li>Item #1</li><li>Item #2</li><ul><li>Item #3</li></ul></ul>", ToHtml(contentText));
    }
    
    [Theory]
    [InlineData("Hi, _Ann_")]
    [InlineData("Hi, *Ann*")]
    public void ItalicItems_ShouldBeRendered_Always(string text)
    {
        Assert.Equal("<p>Hi, <em>Ann</em></p>", ToHtml(text));
    }
    
    [Theory]
    [InlineData("Hi, __Ann__")]
    [InlineData("Hi, **Ann**")]
    public void BoldItems_ShouldBeRendered_Always(string text)
    {
        Assert.Equal("<p>Hi, <b>Ann</b></p>", ToHtml(text));
    }
    
    [Fact]
    public void Heading_ShouldBeRendered_Always()
    {
        var contentText = "# Hello World";

        Assert.Equal("<h1 id=\"hello-world\">Hello World</h1>", ToHtml(contentText));
    }
    
    [Fact]
    public void CodeBlock_ShouldBeRendered_Always()
    {
        var contentText = @"```csharp
var age = 12;
var limit = 10;
```";

        Assert.Equal("<pre><code class=\"csharp\">var age = 12;\r\nvar limit = 10;</code></pre>", ToHtml(contentText));
    }
    
    [Fact]
    public void Link_ShouldBeRendered_Always()
    {
        var contentText = "The [link](https://google.com/page1.html) inside text"; 

        Assert.Equal("<p>The <a href=\"https://google.com/page1.html\">link</a> inside text</p>", ToHtml(contentText));
    }
    
    [Fact]
    public void Image_ShouldBeRendered_Always()
    {
        var contentText = "![Big mountain](/assets/mountain.jpg \"Everest\")";

        Assert.Equal("<p><img src=\"/assets/mountain.jpg\" title=\"Everest\" alt=\"Big mountain\" /></p>", ToHtml(contentText));
    }
    
    [Fact]
    public void Headers_ShouldBeProcessed_Always()
    {
        var contentText = @"---
tags: [.NET, library]
name: Alex
---";

        var headers = GetHeaders(contentText);
        Assert.Equal(2, headers.Length);
        
        Assert.Equal("tags", headers[0].PropertyName);
        Assert.Equal(new[] { ".NET", "library" }, headers[0].Value);
        
        Assert.Equal("name", headers[1].PropertyName);
        Assert.Equal("Alex", headers[1].Value);
    }

    private string ToHtml(string content)
    {
        var scanner = new MdTokenScanner(content);
        var scanResult = scanner.ScanTokens();
        scanResult.ThrowOnAnyError();
        
        var parser = new MdTokenParser(scanResult.Tokens);
        var parseResult = parser.Parse();
        parseResult.ThrowOnAnyError();

        var transformer = new MarkdownToHtmlTransformer();
        return transformer.Transform(parseResult.Result!);
    }
    
    private MdHeader[] GetHeaders(string content)
    {
        var scanner = new MdTokenScanner(content);
        var scanResult = scanner.ScanTokens();
        scanResult.ThrowOnAnyError();
        
        var parser = new MdTokenParser(scanResult.Tokens);
        var parseResult = parser.Parse();
        parseResult.ThrowOnAnyError();
        
        return parseResult.Result!.Headers;
    }
}
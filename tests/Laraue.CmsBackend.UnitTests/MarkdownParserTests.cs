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

    private string ToHtml(string content)
    {
        var scanner = new MdTokenScanner(content);
        var scanResult = scanner.ScanTokens();
        scanResult.ThrowOnAny();
            
        var parser = new MdTokenParser(scanResult.Tokens);
        var parseResult = parser.Parse();
        parseResult.ThrowOnAny();

        var transformer = new MarkdownToHtmlTransformer();
        return transformer.Transform(parseResult.Result!);
    }
}
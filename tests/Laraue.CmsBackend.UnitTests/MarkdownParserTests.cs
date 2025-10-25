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
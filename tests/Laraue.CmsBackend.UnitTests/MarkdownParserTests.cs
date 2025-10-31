using Laraue.CmsBackend.MarkdownTransformation;
using Laraue.Interpreter.Parsing.Extensions;
using Laraue.Interpreter.Scanning.Extensions;

namespace Laraue.CmsBackend.UnitTests;

public class MarkdownParserTests
{
    private static string _newLine = Environment.NewLine;
    
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
    public void Tables_ShouldBeRendered_WhenHeadersMissing()
    {
        var contentText = @"|                   |                   |
|-------------------|-------------------|
| Cell 1 | Cell 2 | ";

        Assert.Equal("<table><thead><tr><th></th><th></th></tr></thead><tbody><tr><td>Cell 1</td><td>Cell 2</td></tr></tbody></table>", ToHtml(contentText));
    }
    
    [Fact]
    public void Tables_ShouldBeRenderedWithInlineItems_Always()
    {
        var contentText = @"| Name | Link      |
| -- | -------- |
| John | No link |
| Henry | ![mountain](mountain.jpg) |";

        Assert.Equal("<table><thead><tr><th>Name</th><th>Link</th></tr></thead><tbody><tr><td>John</td><td>No link</td></tr><tr><td>Henry</td><td><img src=\"mountain.jpg\" title=\"\" alt=\"mountain\" /></td></tr></tbody></table>", ToHtml(contentText));
    }
    
    [Fact]
    public void OrderedLists_ShouldBeRendered_WhenStructureIsHierarchical()
    {
        var contentText = @"1. Item #1
1. Item #2
    1. Item #3";

        Assert.Equal("<ol><li>Item #1</li><li>Item #2</li><ol><li>Item #3</li></ol></ol>", ToHtml(contentText));
    }
    
    [Fact]
    public void OrderedLists_ShouldBeRendered_WhenContentIsMixed()
    {
        var contentText = @"1. Item #1
Description
1. Item #2

## Heading
And text";

        Assert.Equal("<ol><li>Item #1 Description</li><li>Item #2</li></ol><h2 id=\"heading\">Heading</h2><p>And text</p>", ToHtml(contentText));
    }
    
    [Fact]
    public void UnorderedLists_ShouldBeRendered_WhenStructureIsHierarchical()
    {
        var contentText = @"- Item #1
- Item #2  
Hi
    - Item #3";

        Assert.Equal($"<ul><li>Item #1</li><li>Item #2{_newLine}Hi</li><ul><li>Item #3</li></ul></ul>", ToHtml(contentText));
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
    public void BoldItems_ShouldBeRendered_WhenInsideLists()
    {
        var contentText = @"List title
1. **First:** item
2. **Second:** item";
        
        Assert.Equal("<p>List title</p><ol><li><b>First:</b> item</li><li><b>Second:</b> item</li></ol>", ToHtml(contentText));
    }
    
    [Fact]
    public void Carries_ShouldBeRendered_Always()
    {
        var contentText = @"Hello guys,
and girls";

        Assert.Equal("<p>Hello guys, and girls</p>", ToHtml(contentText));
    }
    
    [Fact]
    public void CodeBlock_ShouldBeRendered_Always()
    {
        var contentText = @"```csharp
var age = 12;
var limit = 10;
```";

        Assert.Equal($"<pre><code class=\"csharp\">var age = 12;{_newLine}var limit = 10;</code></pre>", ToHtml(contentText));
    }
    
    
    [Fact]
    public void NestedCodeBlocks_ShouldKeepFormatting_Always()
    {
        var contentText = @"```json
[
 [
  ""Cell1"",
  ""Cell2""
 ]
]
```";

        Assert.Equal($"<pre><code class=\"json\">[{_newLine} [{_newLine}  \"Cell1\",{_newLine}  \"Cell2\"{_newLine} ]{_newLine}]</code></pre>", ToHtml(contentText));
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
        var contentText = @"![Big mountain](/assets/mountain.jpg ""Everest"")
![Small mountain](/assets/mini-mountain.jpg ""Elbrus"")";

        Assert.Equal("<p><img src=\"/assets/mountain.jpg\" title=\"Everest\" alt=\"Big mountain\" /><img src=\"/assets/mini-mountain.jpg\" title=\"Elbrus\" alt=\"Small mountain\" /></p>", ToHtml(contentText));
    }
    
    [Fact]
    public void BlocksAfterInlineElement_ShouldBeRendered_Always()
    {
        var contentText = @"![mountain](mountain.jpg)
## Next title";

        Assert.Equal("<p><img src=\"mountain.jpg\" title=\"\" alt=\"mountain\" /></p><h2 id=\"next-title\">Next title</h2>", ToHtml(contentText));
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
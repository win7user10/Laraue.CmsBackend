using System.Text;
using Laraue.CmsBackend.MarkdownTransformation;

namespace Laraue.CmsBackend.UnitTests;

public class MdTokenScannerTests
{
    [Fact]
    public void TestMdScanner()
    {
        const string markdownFile = @"---
tags: [tag1, tag2]
project: project1
type: unitTestArticle
---

# Title of level 1

## Level 2 title
```csharp
var item = new Item();
```
Hi, _Italic_ __bold__ `font`  next string;
1. List 1 item 1
2. List 1 item 2
    3. List 1 subitem 1

- List 2 item 1
- List 2 item 2
    - List 2 subitem 1

| Name | Surname |
| ---  | ------- |
| Alex | Kent    |";

        var scanner = new MdTokenScanner(markdownFile);
        var result = scanner.ScanTokens();

        var mdTokenParser = new MdTokenParser(result.Tokens);
        var parseResult = mdTokenParser.Parse();

        var sb = new StringBuilder();
        new MdTokenExpressionWriter().Write(sb, parseResult.Result ?? throw new InvalidOperationException());
    }
}
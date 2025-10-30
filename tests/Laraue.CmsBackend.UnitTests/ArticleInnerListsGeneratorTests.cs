using Laraue.CmsBackend.MarkdownTransformation;
using Laraue.Interpreter.Scanning;

namespace Laraue.CmsBackend.UnitTests;

public class ArticleInnerListsGeneratorTests
{
    private readonly ArticleInnerLinksGenerator _generator = new ();
    
    [Fact]
    public void Links_ShouldBeGenerated_Always()
    {
        var tree = new MarkdownTree
        {
            Content =
            [
                new HeadingBlock(1, [GetPlainElement(ParsedMdTokenType.Word, "Title")]),
                new HeadingBlock(2, [GetPlainElement(ParsedMdTokenType.Word, "Subtitle")]),
            ],
            Headers = []
        };

        var links = _generator.ParseLinks(tree);
        Assert.Equal(2, links.Count);
    }

    private PlainElement GetPlainElement(ParsedMdTokenType tokenType, string literal)
    {
        return new PlainElement(tokenType, literal);
    }
}
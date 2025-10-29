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
                new HeadingBlock(1, [GetPlainElement(MdTokenType.Word, "Title")]),
                new HeadingBlock(2, [GetPlainElement(MdTokenType.Word, "Subtitle")]),
            ],
            Headers = []
        };

        var links = _generator.ParseLinks(tree);
        Assert.Equal(2, links.Count);
    }

    private PlainElement GetPlainElement(MdTokenType tokenType, string literal)
    {
        return new PlainElement(new Token<MdTokenType>()
        {
            TokenType = tokenType,
            StartPosition = 1,
            EndPosition = 2,
            Lexeme = "",
            Literal = literal,
            LineNumber = 1,
        });
    }
}
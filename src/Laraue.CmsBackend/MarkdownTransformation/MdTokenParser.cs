using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Laraue.Interpreter.Parsing;
using Laraue.Interpreter.Scanning;

namespace Laraue.CmsBackend.MarkdownTransformation;

public class MdTokenParser : TokenParser<MdTokenType, MarkdownTree>
{
    public MdTokenParser(Token<MdTokenType>[] tokens)
        : base(tokens)
    {
    }

    protected override MarkdownTree ParseInternal()
    {
        MdHeader[] headers = [];
        var contentBlocks = new List<ContentBlock>();
        
        // Try parse headers if they are exist
        Skip(MdTokenType.NewLine);
        if (CheckSequential(MdTokenType.MinusSign, 3))
        {
            headers = ConsumeHeaders();
        }

        ContentBlock? lastContentBlock = null;
        while (!IsParseCompleted)
        {
            var block = ReadNewLineBlock();
            if (block is PlainBlock plainBlock && lastContentBlock is PlainBlock previousPlainBlock)
            {
                previousPlainBlock.Elements = previousPlainBlock.Elements
                    .Append(new PlainElement(ParsedMdTokenType.NewLine))
                    .Concat(plainBlock.Elements)
                    .ToList()
                    .Adjust();
            }
            else
            {
                lastContentBlock = block;
                contentBlocks.Add(lastContentBlock);
            }
        }

        return new MarkdownTree
        {
            Headers = headers,
            Content = contentBlocks.ToArray(),
        };
    }

    private ContentBlock ReadNewLineBlock()
    {
        if (Match(MdTokenType.NewLine))
        {
            return new NewLineBlock();
        }
        
        return ReadTableBlock();
    }

    private ContentBlock ReadTableBlock()
    {
        if (!Check(MdTokenType.Pipe))
        {
            return ReadHeadingBlock();
        }

        // It is the table row only if the next row starts with '|'.
        var nextNewLineOffset = GetNextOffset(MdTokenType.NewLine);
        if (nextNewLineOffset == null || !Check(nextNewLineOffset.Value + 1, MdTokenType.Pipe))
        {
            return ReadHeadingBlock();
        }

        var header = ReadTableRow();
        var tableSetup = ReadTableRow(); // TODO - table align
        
        var rows = new List<TableRow>();
        while (Check(MdTokenType.Pipe))
        {
            rows.Add(ReadTableRow());
        }

        return new TableBlock(header, rows.ToArray());
    }

    // TODO - read header raw
    private TableRow ReadTableRow()
    {
        var cells = new List<TableCell>();

        Consume(MdTokenType.Pipe, "'|' excepted");
        
        while (!IsParseCompleted && !Match(MdTokenType.NewLine))
        {
            var beforePipeElements = ReadInlineElements(ReadInlineMode.StopOnNewLine, MdTokenType.Pipe);
            if (Previous().TokenType == MdTokenType.NewLine)
            {
                break;
            }

            if (Previous().TokenType == MdTokenType.Pipe)
            {
                cells.Add(new TableCell(beforePipeElements.Trim()));
            }
        }
        
        return new TableRow(cells.ToArray());
    }

    private MdElement[] GetTrimmedLineElements(params MdTokenType?[] stopTokens)
    {
        var content = new List<MdElement>();
        int? lastNonWhitespaceElementNumber = null;
        
        while (!IsParseCompleted && !Match(MdTokenType.NewLine) && !Match(stopTokens))
        {
            // Prevent writing whitespaces at the end of string
            if (Check(MdTokenType.Whitespace) && lastNonWhitespaceElementNumber == null)
                lastNonWhitespaceElementNumber = content.Count;
            else
                lastNonWhitespaceElementNumber = null;
            
            var nextElement = ReadInlineElement();
            if (nextElement is null)
            {
                continue;
            }
            content.Add(nextElement);
        }

        var result = lastNonWhitespaceElementNumber != null
            ? content.Take(lastNonWhitespaceElementNumber.Value).ToArray()
            : content.ToArray();

        return result;
    }
    
    private ContentBlock ReadHeadingBlock()
    {
        if (!Match(MdTokenType.NumberSign))
        {
            return ReadCodeBlock();
        }
        
        // Read code block
        var headingLevel = 0;
        do
        {
            headingLevel++;
        } while (Match(MdTokenType.NumberSign));
        
        Skip(MdTokenType.Whitespace);
        var headingElements = GetTrimmedLineElements();
        return new HeadingBlock(headingLevel, headingElements);
    }

    private ContentBlock ReadCodeBlock()
    {
        if (!MatchSequential(MdTokenType.Backtick, 3))
        {
            return ReadList();
        }
        
        // Read code block
        var codeBlocks = new List<MdElement>();
        string? language = null;

        if (Match(MdTokenType.Word))
        {
            language = Previous().Literal as string;
        }
        
        Consume(MdTokenType.NewLine, "Excepted new line after code block definition");
        
        while (!IsParseCompleted)
        {
            if (!MatchSequential(MdTokenType.NewLine, MdTokenType.Backtick, MdTokenType.Backtick, MdTokenType.Backtick))
            {
                var next = ReadInlineElement();
                codeBlocks.Add(next ?? ToPlainElement(Previous()));
            }
            else
            {
                break;
            }
        }
        
        if (!IsParseCompleted)
            Consume(MdTokenType.NewLine, "Excepted new line after code block finished");
        
        return new CodeBlock(language, codeBlocks.ToArray());
    }
    
    private ContentBlock ReadList()
    {
        if (ReadList([MdTokenType.Number, MdTokenType.Dot, MdTokenType.Whitespace], out var result))
        {
            return new OrderedListBlock(result);
        }

        if (ReadList([MdTokenType.MinusSign, MdTokenType.Whitespace], out result))
        {
            return new UnorderedListBlock(result);
        }
        
        return ReadPlain();
    }

    private bool ReadList(MdTokenType[] startListTokensSequence, [NotNullWhen(true)] out ElementsWithIdent[]? result)
    {
        if (!MatchSequential(startListTokensSequence))
        {
            result = null;
            return false;
        }
        
        // Read an ordered list
        var listBlocks = new List<ElementsWithIdent>();
        var nextIdent = 0;

        do
        {
            var currentIdent = nextIdent;
            nextIdent = 0;
            
            // Parse list item
            var listItemElements = new List<MdElement>();
            
            parseListItems:
            
            // Read all tokens until the row end
            listItemElements.AddRange(ReadInlineElements(ReadInlineMode.StopOnNewLine));
            if (Previous().TokenType == startListTokensSequence.Last())
            {
                listItemElements.Add(new PlainElement(ParsedMdTokenType.NewLine));
                goto parseListItems;
            }
            
            // If new line found after new line, the list is finished
            if (Match(MdTokenType.NewLine))
            {
                listBlocks.Add(new ElementsWithIdent(currentIdent / 4, listItemElements.Adjust()));
                break;
            }

            // each ident increase sublevel of the next item
            while (!IsParseCompleted && MatchSequential(MdTokenType.Whitespace))
            {
                nextIdent++;
            }

            if (!CheckSequential(startListTokensSequence) && !IsParseCompleted)
            {
                listItemElements.Add(new PlainElement(ParsedMdTokenType.NewLine));
                goto parseListItems;
            }
            
            listBlocks.Add(new ElementsWithIdent(currentIdent / 4, listItemElements.Adjust()));
            
        } while (!IsParseCompleted && MatchSequential(startListTokensSequence));
        
        result = listBlocks.ToArray();
        return true;
    }
    
    // Read one block once then group them
    private PlainBlock ReadPlain()
    {
        return new PlainBlock
        {
            Elements = ReadInlineElements(ReadInlineMode.StopOnNewLine | ReadInlineMode.StopOnWhitespace),
        };
    }

    [Flags]
    public enum ReadInlineMode
    {
        StopOnNewLine = 1,
        StopOnWhitespace = 2,
    }
    
    private MdElement[] ReadInlineElements(ReadInlineMode readInlineMode, params MdTokenType?[] stopTokens)
    {
        var result = new List<MdElement>();
        while (
            !IsParseCompleted
            && (!readInlineMode.HasFlag(ReadInlineMode.StopOnNewLine) || !Match(MdTokenType.NewLine))
            && !Match(stopTokens)
            && (!readInlineMode.HasFlag(ReadInlineMode.StopOnWhitespace) ||!MatchSequential(MdTokenType.Whitespace, 2)))
        {
            if (Check(MdTokenType.LeftSquareBracket))
            {
                result.AddRange(ReadLinkElements());
                continue;
            }

            if (CheckSequential(MdTokenType.Not, MdTokenType.LeftSquareBracket))
            {
                result.AddRange(ReadImageElements());
                continue;
            }
            
            var next = ReadInlineElement();
            if (next != null)
            {
                result.Add(next);
            }
            else
            {
                break;
            }
        }
        
        return result.ToArray();
    }
    
    private MdElement[] ReadLinkElements()
    {
        Advance(1);
        
        var possibleTitleText = GetTrimmedLineElements(MdTokenType.RightSquareBracket);
        if (Previous().TokenType == MdTokenType.NewLine || !Match(MdTokenType.LeftParenthesis))
        {
            return possibleTitleText;
        }

        var hrefElements = new List<MdElement>();
        while(true)
        {
            var nextElement = ReadPlainElement();
            if (nextElement == null || nextElement.TokenType == ParsedMdTokenType.NewLine)
            {
                return possibleTitleText.Union(hrefElements).ToArray();
            }
            
            if (nextElement.Literal is ")")
            {
                var link = new LinkElement(possibleTitleText, hrefElements.ToArray());
                return [link];
            }
            
            hrefElements.Add(nextElement);
        }
    }

    private MdElement[] ReadImageElements()
    {
        Advance(2);
            
        var possibleAltText = GetTrimmedLineElements(MdTokenType.RightSquareBracket);
        if (Previous().TokenType == MdTokenType.NewLine)
        {
            return possibleAltText;
        }

        if (!Match(MdTokenType.LeftParenthesis))
        {
            return possibleAltText;
        }
            
        var possibleHref = GetTrimmedLineElements(MdTokenType.RightParenthesis, MdTokenType.Quote);
        var possibleTitle = Array.Empty<MdElement>();

        var previousTokenType = Previous().TokenType;
        if (previousTokenType == MdTokenType.Quote)
        {
            possibleTitle = GetTrimmedLineElements(MdTokenType.Quote);
            GetTrimmedLineElements(MdTokenType.RightParenthesis);
            GetTrimmedLineElements(); // Rude iteration to the end of line
        }
            
        var link = new ImageElement(possibleTitle, possibleHref, possibleAltText);
        return [link];
    }

    private MdElement? ReadInlineElement()
    {
        return ReadInlineBacktick();
    }
    
    private MdElement? ReadInlineBacktick()
    {
        if (!Match(MdTokenType.Backtick))
        {
            return ReadInlineBold();
        }

        return new InlineCodeElement();
    }
    
    private MdElement? ReadInlineBold()
    {
        if (Match(MdTokenType.DoubleAsterisk))
        {
            return new BoldAsAsteriskElement();
        }

        if (Match(MdTokenType.DoubleUnderscore))
        {
            return new BoldAsUnderscoreElement();
        }

        return ReadInlineItalic();
    }
    
    private MdElement? ReadInlineItalic()
    {
        if (Match(MdTokenType.Asterisk))
        {
            return new ItalicAsAsteriskElement();
        }

        if (Match(MdTokenType.Underscore))
        {
            return new ItalicAsUnderscoreElement();
        }
        
        return ReadPlainElement();
    }

    private PlainElement? ReadPlainElement()
    {
        return IsParseCompleted
            ? null
            : ToPlainElement(Advance());
    }

    private PlainElement ToPlainElement(Token<MdTokenType> token)
    {
        ParsedMdTokenType? type = token.TokenType switch
        {
            MdTokenType.NewLine => ParsedMdTokenType.NewLine,
            MdTokenType.Whitespace => ParsedMdTokenType.Space,
            null => null,
            _ => ParsedMdTokenType.Word
        };
        
        return new PlainElement(type, token.Literal ?? token.Lexeme);
    }
    
    private bool MatchSequential(MdTokenType tokenType, int count)
    {
        if (CheckSequential(tokenType, count))
        {
            Advance(count);
            return true;
        }
        
        return false;
    }
    
    private bool MatchSequential(params MdTokenType[] tokenTypes)
    {
        if (CheckSequential(tokenTypes))
        {
            Advance(tokenTypes.Length);
            return true;
        }
        
        return false;
    }

    private MdHeader[] ConsumeHeaders()
    {
        var result = new List<MdHeader>();
        
        Advance(3);

        while (!CheckSequential(MdTokenType.MinusSign, 3))
        {
            // Empty header lines are acceptable, skip them
            Skip(MdTokenType.NewLine);
            
            // Whitespaces before property name are acceptable
            Skip(MdTokenType.Whitespace);
            
            // Property name is one word
            var property = Consume(MdTokenType.Word, "Excepted identifier");
            
            // Whitespaces after property name are acceptable
            Skip(MdTokenType.Whitespace);
            Consume(MdTokenType.Delimiter, "Excepted delimiter");
            
            // Whitespaces before property value are acceptable
            Skip(MdTokenType.Whitespace);
            
            var headerValue = ConsumeHeaderValue();
            
            result.Add(new MdHeader
            {
                PropertyName = property.Lexeme!,
                Value = headerValue,
                LineNumber = property.LineNumber,
            });
        }
        
        Advance(3);
        Skip(MdTokenType.Whitespace);
        
        // Require new line after header definition
        if (!IsParseCompleted)
            Consume(MdTokenType.NewLine, "New line after headers section excepted");
        
        return result.ToArray();
    }

    private object? ConsumeHeaderValue()
    {
        if (!Check(MdTokenType.LeftSquareBracket))
        {
            var sb = new StringBuilder();
            var elements = GetTrimmedLineElements();
            foreach (var element in elements.OfType<PlainElement>())
            {
                sb.Append(element.Literal);
            }
            
            return sb.ToString();
        }

        Advance();
        var values = new List<string>();
        
        while (true)
        {
            // Skip each element whitespaces at the line begin
            Skip(MdTokenType.Whitespace);
            
            var sb = new StringBuilder();
            
            // Get all line elements until comma or array end meets, add met elements as plain string to the result array
            var elements = GetTrimmedLineElements(MdTokenType.Comma, MdTokenType.RightSquareBracket);
            foreach (var element in elements.OfType<PlainElement>())
                sb.Append(element.Literal);
            values.Add(sb.ToString());
            
            var lastToken = Previous();
            switch (lastToken.TokenType)
            {
                case MdTokenType.RightSquareBracket:
                    // Array is finished, ensure no tokens more defined
                    Skip(MdTokenType.Whitespace);
                    Consume(MdTokenType.NewLine, "New line after header section excepted");
                    return values.ToArray();
                    // Continue elements scan
                case MdTokenType.Comma:
                    continue;
                default:
                    // Array was started but not finished
                    throw Error(lastToken, "Excepted end of array list");
            }
        }
    }
}

public class MarkdownTree
{
    public required MdHeader[] Headers { get; set; }
    public required ContentBlock[] Content { get; set; }
}

public class MdHeader
{
    public required string PropertyName { get; set; }
    public required object? Value { get; set; }
    public required int LineNumber { get; set; }
}

public abstract record ContentBlock;
public record CodeBlock(string? Language, MdElement[] Elements) : ContentBlock;
public record OrderedListBlock(ElementsWithIdent[] Elements) : ContentBlock;
public record UnorderedListBlock(ElementsWithIdent[] Elements) : ContentBlock;
public record ElementsWithIdent(int Ident, MdElement[] Elements);
public record HeadingBlock(int Level, MdElement[] Elements) : ContentBlock;

public record PlainBlock : ContentBlock
{
    public override string ToString()
    {
        return string.Concat(Elements.Select(r => r.ToString()));
    }

    public required MdElement[] Elements { get; set; }
}

public record NewLineBlock : ContentBlock;
public record TableBlock(TableRow Header, TableRow[] Rows) : ContentBlock
{
    public override string ToString()
    {
        return string.Join(Environment.NewLine, Rows.Prepend(Header).Select(r => r.ToString()));
    }
}

public record TableRow(TableCell[] Cells)
{
    public override string ToString()
    {
        return string.Join("|", Cells.Select(r => r.ToString()));
    }
}

public record TableCell(MdElement[] Elements)
{
    public override string ToString()
    {
        return string.Concat(Elements.Select(r => r.ToString()));
    }
}
public abstract record MdElement;

[DebuggerDisplay("{TokenType} \"{ToString()}\"")]
public record PlainElement : MdElement
{
    public ParsedMdTokenType? TokenType { get; }
    public object? Literal { get; }

    public PlainElement(ParsedMdTokenType? tokenType, object? literal = null)
    {
        TokenType = tokenType;
        Literal = literal;
    }
    
    public override string ToString()
    {
        return Literal?.ToString() ?? string.Empty;
    }
}

public enum ParsedMdTokenType
{
    NewLine,
    Space,
    Word,
}

public record LinkElement(MdElement[] Title, MdElement[] Href) : MdElement;
public record ImageElement(MdElement[] Title, MdElement[] Href, MdElement[] Alt) : MdElement;
public record BoldAsUnderscoreElement : MdElement;
public record BoldAsAsteriskElement : MdElement;
public record ItalicAsUnderscoreElement : MdElement;
public record ItalicAsAsteriskElement : MdElement;
public record InlineCodeElement : MdElement;
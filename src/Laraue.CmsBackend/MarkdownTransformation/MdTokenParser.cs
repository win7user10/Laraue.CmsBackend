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
        
        while (!IsParseCompleted)
        {
            contentBlocks.Add(ReadNewLineBlock());
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
            Skip(MdTokenType.Whitespace);

            var elements = GetTrimmedLineElements(MdTokenType.Pipe);
            
            cells.Add(new TableCell(elements));
        }
        
        return new TableRow(cells.ToArray());
    }

    private MdElement[] GetTrimmedLineElements(params MdTokenType?[] stopTokens)
    {
        var content = new List<MdElement>();
        int? lastNonWhitespaceElementNumber = null;
        
        do
        {
            // Prevent writing whitespaces at the end of string
            if (Check(MdTokenType.Whitespace) && lastNonWhitespaceElementNumber == null)
                lastNonWhitespaceElementNumber = content.Count;
            else
                lastNonWhitespaceElementNumber = null;
                
            var nextElement = ReadInlineElement()!;
            content.Add(nextElement);
                
        } while (!IsParseCompleted && !Match(MdTokenType.NewLine) && !Match(stopTokens));

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
        if (!CheckSequential(MdTokenType.Backtick, 3))
        {
            return ReadOrderedList();
        }
        
        Advance(3);
        
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
            codeBlocks.AddRange(GetTrimmedLineElements(MdTokenType.Backtick));
            var previous = Previous();
            
            var requiredBackticks = previous.TokenType == MdTokenType.Backtick ? 2 : 3;
            if (CheckSequential(MdTokenType.Backtick, requiredBackticks))
            {
                Advance(requiredBackticks);
                break;
            }
            
            // Handle all new lines in code block before block finish met
            if (previous.TokenType == MdTokenType.NewLine)
            {
                codeBlocks.Add(new PlainElement(previous));
            }
        }
        
        if (!IsParseCompleted)
            Consume(MdTokenType.NewLine, "Excepted new line after code block finished");
        
        return new CodeBlock(language, codeBlocks.ToArray());
    }
    
    private ContentBlock ReadOrderedList()
    {
        if (!CheckSequential(MdTokenType.Number, MdTokenType.Dot, MdTokenType.Whitespace))
        {
            return ReadUnorderedList();
        }
        
        // Read ordered list
        var listBlocks = new List<ContentWithIdent>();
        var nextIdent = 0;
        do
        {
            Advance(3);
            listBlocks.Add(new ContentWithIdent(nextIdent, ReadInlineElements()));
            nextIdent = 0;
            while (Match(MdTokenType.Ident))
                nextIdent++;
        } while (CheckSequential(MdTokenType.Number, MdTokenType.Dot, MdTokenType.Whitespace));
        
        return new OrderedListBlock(listBlocks.ToArray());
    }

    private ContentBlock ReadUnorderedList()
    {
        if (!CheckSequential(MdTokenType.MinusSign, MdTokenType.Whitespace))
        {
            return ReadPlain();
        }
        
        // Read unordered list
        var listBlocks = new List<ContentWithIdent>();
        var nextIdent = 0;
        do
        {
            Advance(2);
            listBlocks.AddRange(new ContentWithIdent(nextIdent, ReadInlineElements()));
            nextIdent = 0;
            while (Match(MdTokenType.Ident))
                nextIdent++;
        } while (CheckSequential(MdTokenType.MinusSign, MdTokenType.Whitespace));
        
        return new UnorderedListBlock(listBlocks.ToArray());
    }
    
    // Read one block once then group them
    private PlainBlock ReadPlain()
    {
        return new PlainBlock(ReadImageElements());
    }

    private MdElement[] ReadImageElements()
    {
        if (CheckSequential(MdTokenType.Not, MdTokenType.LeftSquareBracket))
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
            if (Previous().TokenType == MdTokenType.Quote)
            {
                possibleTitle = GetTrimmedLineElements(MdTokenType.Quote);
            }

            _ = GetTrimmedLineElements(MdTokenType.RightParenthesis);
            
            
            var link = new ImageElement(possibleTitle, possibleHref, possibleAltText);
            return ReadInlineElements().Prepend(link).ToArray();
        }
        
        return ReadLinkElements();
    }
    
    private MdElement[] ReadLinkElements()
    {
        if (Match(MdTokenType.LeftSquareBracket))
        {
            var possibleTitleText = GetTrimmedLineElements(MdTokenType.RightSquareBracket);
            if (Previous().TokenType == MdTokenType.NewLine)
            {
                return possibleTitleText;
            }

            if (!Match(MdTokenType.LeftParenthesis))
            {
                return possibleTitleText;
            }
            
            var possibleHref = GetTrimmedLineElements(MdTokenType.RightParenthesis);
            var link = new LinkElement(possibleTitleText, possibleHref);
            return ReadInlineElements().Prepend(link).ToArray();
        }
        
        return ReadInlineElements();
    }
    
    private MdElement[] ReadInlineElements()
    {
        var result = new List<MdElement>();
        do
        {
            var next = ReadInlineElement();
            if (next != null)
            {
                result.Add(next);
            }
            else
            {
                break;
            }
        } while (!IsParseCompleted && !Match(MdTokenType.LineBreak, MdTokenType.NewLine));
        
        return result.ToArray();
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
            : Match(MdTokenType.LineBreak) 
                ? null // New line
                : new PlainElement(Advance());
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
                sb.Append(element.Source.Lexeme);
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
                sb.Append(element.Source.Lexeme);
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

public record MdHeaderValue(object? Value);
public abstract record ContentBlock;
public record CodeBlock(string? Language, MdElement[] Elements) : ContentBlock;
public record OrderedListBlock(ContentWithIdent[] Elements) : ContentBlock;
public record UnorderedListBlock(ContentWithIdent[] Elements) : ContentBlock;
public record ContentWithIdent(int Ident, MdElement[] Elements);
public record HeadingBlock(int Level, MdElement[] Elements) : ContentBlock;

public record PlainBlock(MdElement[] Elements) : ContentBlock
{
    public override string ToString()
    {
        return string.Concat(Elements.Select(r => r.ToString()));
    }
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

public record PlainElement(Token<MdTokenType> Source) : MdElement
{
    public override string ToString()
    {
        return Source.Literal?.ToString() ?? string.Empty;
    }
}

public record LinkElement(MdElement[] Title, MdElement[] Href) : MdElement;
public record ImageElement(MdElement[] Title, MdElement[] Href, MdElement[] Alt) : MdElement;
public record BoldAsUnderscoreElement : MdElement;
public record BoldAsAsteriskElement : MdElement;
public record ItalicAsUnderscoreElement : MdElement;
public record ItalicAsAsteriskElement : MdElement;
public record InlineCodeElement : MdElement;
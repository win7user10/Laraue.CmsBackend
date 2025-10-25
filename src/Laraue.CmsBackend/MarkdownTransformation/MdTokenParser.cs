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
                
            } while (!IsParseCompleted && !Match(MdTokenType.Pipe, MdTokenType.NewLine));

            var result = lastNonWhitespaceElementNumber != null
                ? content.Take(lastNonWhitespaceElementNumber.Value)
                : content.ToArray();
            
            cells.Add(new TableCell(result.ToArray()));
        }
        
        return new TableRow(cells.ToArray());
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
        
        // Read inline
        var inner = ReadInline();
        return new HeadingBlock(headingLevel, inner);
    }

    private ContentBlock ReadCodeBlock()
    {
        if (!CheckSequential(MdTokenType.Backtick, 3))
        {
            return ReadOrderedList();
        }
        
        Advance(3);
        
        // Read code block
        var codeBlocks = new List<ContentBlock>();
        string? language = null;

        if (Match(MdTokenType.Word))
        {
            language = Previous().Literal as string;
        }
        
        Consume(MdTokenType.NewLine, "Excepted new line after code block definition");
        
        do
        {
            codeBlocks.AddRange(ReadInline());
        } while (!CheckSequential(MdTokenType.Backtick, 3));
        
        Advance(3);
        
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
            return ReadInline();
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
    private PlainBlock ReadInline()
    {
        return new PlainBlock(ReadInlineElements());
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
            // New lines doesn't matter here
            Skip(MdTokenType.NewLine);
            
            var property = Consume(MdTokenType.Word, "Excepted identifier");
            
            Skip(MdTokenType.Whitespace);
            Consume(MdTokenType.Delimiter, "Excepted delimiter");
            Skip(MdTokenType.Whitespace);
            
            var headerValue = ConsumeHeaderValue();
            Skip(MdTokenType.Whitespace);
            var token = Consume(MdTokenType.NewLine, "New line after property definition excepted");
            
            result.Add(new MdHeader
            {
                PropertyName = property.Lexeme!,
                Value = headerValue,
                LineNumber = token.LineNumber,
            });
        }
        
        Advance(3);
        Skip(MdTokenType.Whitespace);
        Consume(MdTokenType.NewLine, "New line after headers section excepted");
        
        return result.ToArray();
    }

    private object? ConsumeHeaderValue()
    {
        if (!Check(MdTokenType.StartArray))
        {
            var sb = new StringBuilder();
            
            while (!IsParseCompleted && !Check(MdTokenType.NewLine))
            {
                var value = Advance()!;
                sb.Append(value.Lexeme);
            }

            return sb.ToString();
        }

        Advance();
        var values = new List<string>();
        
        while (!IsParseCompleted && !Check(MdTokenType.EndArray) && !Check(MdTokenType.NewLine))
        {
            var sb = new StringBuilder();
            while (!IsParseCompleted && !Match(MdTokenType.Comma) && !Check(MdTokenType.EndArray) && !Check(MdTokenType.NewLine))
            {
                var value = Advance()!;
                sb.Append(value.Lexeme);
            }
            
            values.Add(sb.ToString());
        }
        
        Consume(MdTokenType.EndArray, "Excepted end of array list");
        
        return values.ToArray();
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
public record CodeBlock(string? Language, ContentBlock[] Elements) : ContentBlock;
public record OrderedListBlock(ContentWithIdent[] Elements) : ContentBlock;
public record UnorderedListBlock(ContentWithIdent[] Elements) : ContentBlock;
public record ContentWithIdent(int Ident, MdElement[] Elements);
public record HeadingBlock(int Level, ContentBlock Content) : ContentBlock;

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

public record BoldAsUnderscoreElement : MdElement;
public record BoldAsAsteriskElement : MdElement;
public record ItalicAsUnderscoreElement : MdElement;
public record ItalicAsAsteriskElement : MdElement;
public record InlineCodeElement : MdElement;
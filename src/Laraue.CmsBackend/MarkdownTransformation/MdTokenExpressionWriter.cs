using System.Text;
using Laraue.CmsBackend.Utils;

namespace Laraue.CmsBackend.MarkdownTransformation;

public  class MdTokenExpressionWriter
{
    public void Write(StringBuilder sb, MarkdownTree expression)
    {
        Write(sb, expression.Content);
    }

    private void Write(StringBuilder sb, IEnumerable<ContentBlock> contentBlocks)
    {
        foreach (var contentBlock in contentBlocks)
        {
            Write(sb, contentBlock);
        }
    }

    private void Write(StringBuilder sb, ContentBlock contentBlock)
    {
        switch (contentBlock)
        {
            case HeadingBlock headingBlock:
                Write(sb, headingBlock);
                break;
            case CodeBlock codeBlock:
                Write(sb, codeBlock);
                break;
            case PlainBlock plainBlock:
                Write(sb, plainBlock);
                break;
            case UnorderedListBlock unorderedListBlock:
                WriteListBlock(sb, "ul", unorderedListBlock.Elements);
                break;
            case OrderedListBlock orderedListBlock:
                WriteListBlock(sb, "ol", orderedListBlock.Elements);
                break;
            case TableBlock tableBlock:
                Write(sb, tableBlock);
                break;
            case NewLineBlock newLineBlock:
                Write(sb, newLineBlock);
                break;
        }
    }

    private void Write(StringBuilder sb, NewLineBlock newLineBlock)
    {
        sb.Append(Environment.NewLine);
    }
    
    private void WriteListBlock(StringBuilder sb, string listTag, ContentWithIdent[] elements)
    {
        var currentIdentLevel = -1;
        
        foreach (var element in elements)
        {
            if (element.Ident > currentIdentLevel)
            {
                sb.Append($"<{listTag}>");
                currentIdentLevel = element.Ident;
            }
            
            if (element.Ident < currentIdentLevel)
            {
                sb.Append($"</{listTag}>");
                currentIdentLevel = element.Ident;
            }
            
            sb.Append("<li>");
            Write(sb, element.Block);
            sb.Append("</li>");
        }

        while (currentIdentLevel > -1)
        {
            sb.Append($"</{listTag}>");
            currentIdentLevel--;
        }
    }

    private void Write(StringBuilder sb, HeadingBlock headingBlock)
    {
        var innerSb = new StringBuilder();
        Write(innerSb, headingBlock.Content);
        var id = HeadingUtility.GenerateHeadingId(innerSb.ToString());
        
        sb
            .Append($"<h{headingBlock.Level} id=\"")
            .Append(id)
            .Append("\"/>")
            .Append(innerSb)
            .AppendLine($"</h{headingBlock.Level}>");
    }

    private void Write(StringBuilder sb, CodeBlock codeBlock)
    {
        sb.Append("<pre><code");
        
        if (codeBlock.Language is not null)
        {
            sb.Append($" class=\"{codeBlock.Language}\"");
        }
        
        sb.Append('>');
        
        Write(sb, codeBlock.Elements);
        
        sb.AppendLine("</code></pre>");
    }
    
    private void Write(StringBuilder sb, PlainBlock plainBlock)
    {
        sb.Append("<p>");
        Write(sb, plainBlock.Elements);
        sb.Append("</p>");
    }

    private void Write(StringBuilder sb, IEnumerable<MdElement> elements)
    {
        foreach (var element in elements)
        {
            Write(sb, element);
        }
    }

    private void Write(StringBuilder sb, MdElement element)
    {
        switch (element)
        {
            case PlainElement plainElement:
                Write(sb, plainElement);
                break;
            case BoldAsUnderscoreElement:
            case BoldAsAsteriskElement:
                WritePairTag(sb, "b", element);
                break;
            case ItalicAsAsteriskElement:
            case ItalicAsUnderscoreElement:
                WritePairTag(sb, "em", element);
                break;
            case InlineCodeElement:
                WritePairTag(sb, "code", element);
                break;
        }
    }
    
    private void Write(StringBuilder sb, TableBlock tableBlock)
    {
        sb
            .Append("<table>")
            .Append("<thead>")
            .Append("<tr>");

        foreach (var row in tableBlock.Header.Cells)
        {
            sb.Append("<th>");
            Write(sb, row.Elements);
            sb.Append("</th>");
        }
        
        sb
            .Append("</tr>")
            .Append("</thead>")
            .Append("<tbody>");

        foreach (var row in tableBlock.Rows)
        {
            sb.Append("<tr>");
            foreach (var cell in row.Cells)
            {
                sb.Append("<td>");
                Write(sb, cell.Elements);
                sb.Append("</td>");
            }
            sb.Append("</tr>");
        }
        
        sb
            .Append("</tbody>")
            .Append("</table>");
    }

    private readonly HashSet<Type> _openedTags = new ();
    private void WritePairTag(StringBuilder sb, string tag, MdElement plainElement)
    {
        var type = plainElement.GetType();
        
        if (_openedTags.Add(type))
        {
            sb.Append($"<{tag}>");
        }
        else
        {
            _openedTags.Remove(type);
            sb.Append($"</{tag}>");
        }
    }
    
    private void Write(StringBuilder sb, PlainElement plainElement)
    {
        var value = plainElement.Source.TokenType switch
        {
            MdTokenType.Word => plainElement.Source.Literal,
            MdTokenType.LineBreak => Environment.NewLine,
            MdTokenType.Whitespace => " ",
            _ => plainElement.Source.Lexeme
        };
        
        sb.Append(value);
    }
}
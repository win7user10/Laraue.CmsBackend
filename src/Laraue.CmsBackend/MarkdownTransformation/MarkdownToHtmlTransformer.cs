using System.Text;
using Laraue.CmsBackend.Utils;

namespace Laraue.CmsBackend.MarkdownTransformation;

public class MarkdownToHtmlTransformer : ITransformer
{
    public string Transform(string markdown)
    {
        return new InternalReader(markdown).ReadAsHtml();
    }

    private readonly ref struct InternalReader(string markdown)
    {
        private readonly StringBuilder _stringBuilder = new (markdown.Length);

        public string ReadAsHtml()
        {
            var reader = new SpanReader(markdown);
            
            while (true)
            {
                if (reader.IsEndOfFileReached)
                {
                    return _stringBuilder.ToString();
                }
                
                // Skip empty chars
                reader.TakeWhile(c => c == ' ');
                ReadHeading(ref reader);
            }
        }

        private void ReadHeading(ref SpanReader reader)
        {
            if (HeadingUtility.TryReadHeading(ref reader, out var heading))
            {
                var id = HeadingUtility.GenerateHeadingId(heading.Text);
                
                _stringBuilder
                    .Append($"<h{heading.Level} id=\"{id}\">");

                var headingTextReader = new SpanReader(heading.Text);
                ReadRaw(ref headingTextReader);
                
                _stringBuilder
                    .AppendLine($"</h{heading.Level}>");
            }
            else if (!TryReadCodeBlock(ref reader) && !TryReadList(ref reader))
            {
                ReadParagraph(ref reader);
            }
        }
        
        private bool TryReadCodeBlock(ref SpanReader reader)
        {
            const string codeSectionStart = "```";
            
            if (!reader.TryPop(codeSectionStart))
            {
                return false;
            }

            var codeBlock = reader.ReadTo(codeSectionStart);
            var codeBlockReader = new SpanReader(codeBlock);
            
            codeBlockReader.TryReadLine(out var codeBlockLine);
            var language = codeBlockLine;
            
            _stringBuilder.Append("<pre><code");
        
            if (language.Length != 0)
            {
                _stringBuilder.Append($" class=\"{language}\"");
            }

            _stringBuilder.Append(">");
            while (codeBlockReader.TryReadLine(out codeBlockLine))
            {
                var innerReader = new SpanReader(codeBlockLine);
                if (innerReader.StartsWith(codeSectionStart))
                {
                    break;
                }
                
                ReadRaw(ref innerReader);
                _stringBuilder.AppendLine();
            }
            
            _stringBuilder.Append("</code></pre>");

            return true;
        }
        
        private readonly HashSet<char> _intChars = Enumerable.Range('1', 9).Select(i => (char)i).ToHashSet(); 
        private bool TryReadList(ref SpanReader reader)
        {
            if (!TryReadNumber(ref reader))
            {
                return false;
            }

            _stringBuilder.Append("<ul>");
            
            do
            {
                _stringBuilder.Append("<li>");
                var readerLine = reader.ReadLine();
                var innerReader = new SpanReader(readerLine);
                ReadRaw(ref innerReader);
                _stringBuilder.Append("</li>");
            } while (TryReadNumber(ref reader));
            
            _stringBuilder.Append("</ul>");
            
            return true;
        }

        private bool TryReadNumber(ref SpanReader reader)
        {
            if (reader.TryPeekChar(out var nextChar)
                && _intChars.Contains(nextChar)
                && reader.TryPeekChar(1, out var furtherChar)
                && furtherChar == '.')
            {
                reader.Pop();
                reader.Pop();

                return true;
            }

            return false;
        }
        
        private void ReadParagraph(ref SpanReader reader)
        {
            _stringBuilder.Append("<p>");

            var paragraphLine = reader.ReadLine();
            var innerReader = new SpanReader(paragraphLine);
            
            ReadRaw(ref innerReader);
            
            _stringBuilder.AppendLine("</p>");
        }

        private readonly Dictionary<char, string> _escapeMap = new()
        {
            ['<'] = "&lt;",
            ['>'] = "&gt;",
            ['"'] = "&quot;",
            ['\''] = "&#39;",
            ['&'] = "&amp;",
        };
        
        private void ReadRaw(ref SpanReader reader)
        {
            while (!reader.IsEndOfFileReached)
            {
                if (TryReadLink(ref reader))
                {
                    continue;
                }
                
                if (TryReadInlineCode(ref reader))
                {
                    continue;
                }

                var nextChar = reader.Read();
                
                if (_escapeMap.TryGetValue(nextChar, out var escapedChar))
                {
                    _stringBuilder.Append(escapedChar);
                }
                else
                {
                    _stringBuilder.Append(nextChar);
                }
            }
        }

        private bool TryReadLink(ref SpanReader reader)
        {
            if (!reader.TryPeekChar('['))
            {
                return false;
            }
            
            var innerReader = new SpanReader(reader.Source[1..]);
            var linkName = innerReader.TakeWhile(c => c != ']');
            if (linkName.Length == 0)
            {
                return false;
            }

            innerReader.TryPop();
            if (!innerReader.TryPop("("))
            {
                return false;
            }
                
            var linkBody = innerReader.TakeWhile(c => c != ')');
            if (linkBody.Length == 0)
            {
                return false;
            }
            
            innerReader.TryPop();
            
            _stringBuilder.Append($"<a href=\"{linkBody}\">{linkName}</a>");
            reader = innerReader;
            return true;
        }

        private bool TryReadInlineCode(ref SpanReader reader)
        {
            if (!reader.TryPeekChar('`'))
            {
                return false;
            }
            
            var innerReader = new SpanReader(reader.Source[1..]);
            var codeBlock = innerReader.TakeWhile(c => c != '`');
            if (codeBlock.Length == 0)
            {
                return false;
            }
            
            innerReader.TryPop();
            _stringBuilder.Append($"<code>{codeBlock}</code>");
            reader = innerReader;
            return true;
        }
    }
}
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Laraue.CmsBackend.Contracts;
using Laraue.CmsBackend.MarkdownTransformation;

namespace Laraue.CmsBackend;

public interface IMarkdownParser
{
    ParsedMdFile Parse(ContentProperties properties);
}

public class MarkdownParser(
    ITransformer? markdownContentTransformer,
    IArticleInnerLinksGenerator articleInnerLinksGenerator)
    : IMarkdownParser
{
    public ParsedMdFile Parse(ContentProperties properties)
    {
        return new InternalParser(markdownContentTransformer, articleInnerLinksGenerator, properties).Parse();
    }
    
    private class InternalParser(
        ITransformer? markdownContentTransformer,
        IArticleInnerLinksGenerator articleInnerLinksGenerator,
        ContentProperties contentProperties)
    {
        private readonly StringReader _stringReader = new (contentProperties.Markdown);
        private int _lineNumber;
        
        public ParsedMdFile Parse()
        {
            if (!TryReadLine(out var line) || !line.StartsWith("---"))
            {
                throw new MarkdownParserException("Metadata section start is excepted", _lineNumber);
            }

            // Read properties
            var properties = new Dictionary<string, ParsedMdFileProperty>();
            while (true)
            {
                var lineNumber = _lineNumber;
                if (!TryReadLine(out line))
                {
                    throw new MarkdownParserException("Excepted metadata definition or end of section but EOF received", _lineNumber);
                }
                
                if (line.StartsWith("---"))
                {
                    break;
                }
                
                var property = ParseProperty(line, lineNumber);
                properties.Add(property.Name, property);
            }
        
            // Read text
            var content = GetRemainedString();
            var links = articleInnerLinksGenerator.ParseLinks(content);
            
            if (markdownContentTransformer is not null)
            {
                content = markdownContentTransformer.Transform(content);
            }

            var contentType = PopPropertyValueOrThrow("type");
        
            return new ParsedMdFile
            {
                Id = contentProperties.Id,
                ContentType = contentType,
                Content = content,
                UpdatedAt = contentProperties.UpdatedAt,
                CreatedAt = contentProperties.CreatedAt,
                Properties = properties.Values,
                Path = contentProperties.Path,
                InnerLinks = links,
            };

            string PopPropertyValueOrThrow(string key)
            {
                if (!properties.Remove(key, out var value))
                {
                    throw new MarkdownParserException($"Property '{key}' not found", _lineNumber);
                }

                return value!.Value;
            }
        }

        private static readonly Regex PropertyRegex = new("\\s?(\\w+)\\s?:\\s?([^\\n]+)\\s?", RegexOptions.Compiled);
        private ParsedMdFileProperty ParseProperty(string text, int lineNumber)
        {
            var match = PropertyRegex.Match(text);
            if (!match.Success)
            {
                throw new MarkdownParserException($"The string '{text}' does not match the expected format", lineNumber);
            }

            return new ParsedMdFileProperty
            {
                Name = match.Groups[1].Value,
                Value = match.Groups[2].Value,
                SourceLineNumber = lineNumber,
            };
        }
        
        private bool TryReadLine([NotNullWhen(true)] out string? line)
        {
            var nextLine = _stringReader.ReadLine();
            line = nextLine;

            if (line == null)
            {
                return false;
            }
            
            _lineNumber++;
            return true;
        }
    
        private string GetRemainedString()
        {
            return _stringReader.ReadToEnd();
        }
    }
}
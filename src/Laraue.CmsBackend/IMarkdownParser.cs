using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Laraue.CmsBackend;

public interface IMarkdownParser
{
    ParsedMdFile Parse(string markdown, string path, string id, DateTime updateAt);
}

public class MarkdownParser : IMarkdownParser
{
    public ParsedMdFile Parse(string markdown, string path, string id, DateTime updateAt)
    {
        return new InternalParser(markdown, path, id, updateAt).Parse();
    }
    
    private class InternalParser(string markdown, string path, string id, DateTime updateAt)
    {
        private readonly StringReader _stringReader = new (markdown);
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

            var contentType = PopPropertyValueOrThrow("type");
        
            return new ParsedMdFile
            {
                Id = id,
                ContentType = contentType,
                Content = content,
                UpdatedAt = updateAt,
                Properties = properties.Values,
                Path = path,
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

public sealed record ParsedMdFile
{
    public required string Id { get; init; }
    public required string ContentType { get; init; }
    public required string Content { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public required ICollection<ParsedMdFileProperty> Properties { get; init; }
    public required string Path { get; init; }
}

public sealed record ParsedMdFileProperty
{
    public required string Name { get; init; }
    public required string Value { get; init; }
    public int SourceLineNumber { get; init; }
}
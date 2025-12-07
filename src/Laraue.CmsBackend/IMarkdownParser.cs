using Laraue.CmsBackend.Contracts;
using Laraue.CmsBackend.MarkdownTransformation;
using Laraue.Interpreter.Common;
using Laraue.Interpreter.Parsing.Extensions;
using Laraue.Interpreter.Scanning.Extensions;

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
        try
        {
            return new InternalParser(markdownContentTransformer, articleInnerLinksGenerator, properties).Parse();
        }
        catch (CompileException exception)
        {
            throw new MarkdownParserException($"Incorrect markdown: {exception.Message}", exception);
        }
    }
    
    private class InternalParser(
        ITransformer? markdownContentTransformer,
        IArticleInnerLinksGenerator articleInnerLinksGenerator,
        ContentProperties contentProperties)
    {
        private const string IndexFileName = "index";
        
        public ParsedMdFile Parse()
        {
            var scanner = new MdTokenScanner(contentProperties.Markdown);
            var scanResult = scanner.ScanTokens();
            scanResult.ThrowOnAnyError();
            
            var parser = new MdTokenParser(scanResult.Tokens);
            var parseResult = parser.Parse();
            parseResult.ThrowOnAnyError();

            var content = contentProperties.Markdown;
            
            // Read text
            var links = articleInnerLinksGenerator.ParseLinks(parseResult.Result!);
            if (markdownContentTransformer is not null)
            {
                content = markdownContentTransformer.Transform(parseResult.Result!);
            }
            
            var properties = ParseProperties(parseResult.Result!);

            var contentType = properties.Remove("type", out var contentTypeProperty)
                ? contentTypeProperty.Value?.ToString() ?? ContentTypeRegistry.UndefinedContentType
                : ContentTypeRegistry.UndefinedContentType;

            var path = contentProperties.Id == IndexFileName
                ? contentProperties.Path
                : new FilePath(contentProperties.Path.Segments.Append(contentProperties.Id).ToArray());
            
            return new ParsedMdFile
            {
                ContentType = contentType,
                Content = content,
                Properties = properties.Values,
                Path = path,
                InnerLinks = links,
            };
        }
    }

    private static Dictionary<string, ParsedMdFileProperty> ParseProperties(MarkdownTree markdownTree)
    {
        var result = new Dictionary<string, ParsedMdFileProperty>();
        foreach (var header in markdownTree.Headers)
        {
            var property = new ParsedMdFileProperty()
            {
                Value = header.Value,
                Name = header.PropertyName,
                SourceLineNumber = header.LineNumber,
            };
            
            result.Add(header.PropertyName, property);
        }

        return result;
    }
}
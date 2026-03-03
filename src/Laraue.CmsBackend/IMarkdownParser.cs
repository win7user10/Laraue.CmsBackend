using Laraue.CmsBackend.Contracts;
using Laraue.Interpreter.Common;
using Laraue.Interpreter.Markdown;
using Laraue.Interpreter.Markdown.Meta;

namespace Laraue.CmsBackend;

public interface IMarkdownParser
{
    ParsedMdFile Parse(ContentProperties properties);
}

public class MarkdownParser(
    IMarkdownTranspiler markdownTranspiler)
    : IMarkdownParser
{
    private const string IndexFileName = "index";
    
    public ParsedMdFile Parse(ContentProperties contentSource)
    {
        try
        {
            var result = markdownTranspiler.ToHtml(contentSource.Markdown);
            var content = result.HtmlContent;
        
            // Read text
            var links = result.InnerLinks;
            var properties = ParseProperties(result.Headers);

            var contentType = properties.Remove("type", out var contentTypeProperty)
                ? contentTypeProperty.Value?.ToString() ?? ContentTypeRegistry.UndefinedContentType
                : ContentTypeRegistry.UndefinedContentType;

            // TODO - logical path generating can be in separated class. 
            var logicalPath = contentSource.Id == IndexFileName
                ? contentSource.Path
                : new FilePath(contentSource.Path.Segments.Append(contentSource.Id).ToArray());

            var fileName = contentSource.Id == IndexFileName
                ? null
                : contentSource.Id;
            
            return new ParsedMdFile
            {
                FileName = fileName,
                ContentType = contentType,
                Content = content,
                Properties = properties.Values,
                PhysicalPath = contentSource.Path,
                LogicalPath = logicalPath,
                InnerLinks = links,
            };
        }
        catch (CompileException exception)
        {
            throw new MarkdownParserException($"Incorrect markdown: {exception.Message}", exception);
        }
    }

    private static Dictionary<string, ParsedMdFileProperty> ParseProperties(MarkdownHeader[] headers)
    {
        var result = new Dictionary<string, ParsedMdFileProperty>();
        foreach (var header in headers)
        {
            var property = new ParsedMdFileProperty
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
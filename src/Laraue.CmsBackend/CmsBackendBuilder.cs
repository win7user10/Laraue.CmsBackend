using System.Text;
using Laraue.CmsBackend.Contracts;

namespace Laraue.CmsBackend;

public interface ICmsBackendBuilder
{
    ICmsBackendBuilder AddContent(ContentProperties properties);
    ICmsBackendBuilder AddContentType<TContentType>() where TContentType : BaseDocumentType;
    ICmsBackend Build();
}

public class CmsBackendBuilder
    : ICmsBackendBuilder
{
    private readonly IMarkdownParser _markdownParser;
    private readonly IMarkdownProcessor _markdownProcessor;
    
    private readonly ContentTypeRegistry _contentTypeRegistry = new();
    private readonly ParsedMdFileRegistry _parsedMdFileRegistry = new();

    public CmsBackendBuilder(IMarkdownParser markdownParser, IMarkdownProcessor markdownProcessor)
    {
        _markdownParser = markdownParser;
        _markdownProcessor = markdownProcessor;

        AddContentType<DefaultDocumentType>();
    }
    
    public ICmsBackendBuilder AddContent(ContentProperties properties)
    {
        var result = _markdownParser.Parse(properties);
        
        _parsedMdFileRegistry.Add(result);
        
        return this;
    }

    public ICmsBackendBuilder AddContentType<TContentType>() where TContentType : BaseDocumentType
    {
        _contentTypeRegistry.AddContentType<TContentType>();
        
        return this;
    }

    public ICmsBackend Build()
    {
        var buildResult = _markdownProcessor.ApplyRegistrySchemas(
            _parsedMdFileRegistry.Values,
            _contentTypeRegistry);

        if (buildResult.Success)
        {
            return new CmsBackendUnit(buildResult.MarkdownFiles);
        }

        throw new CmsBackendException(buildResult.Errors);
    }
}

public class CmsBackendException : Exception
{
    public readonly IDictionary<ParsedMdFile, ICollection<ValidationError>> Exceptions;

    public CmsBackendException(IDictionary<ParsedMdFile, ICollection<ValidationError>> exceptions)
        : base(GetMessage(exceptions))
    {
        Exceptions = exceptions;
    }

    private static string GetMessage(IDictionary<ParsedMdFile, ICollection<ValidationError>> exceptions)
    {
        var sb = new StringBuilder("Exception while CMS constructing");

        sb.AppendLine();
        
        foreach (var exceptionByEntity in exceptions)
        {
            sb.AppendLine()
                .Append("Entity ")
                .Append("path: '")
                .Append(string.Join(Path.DirectorySeparatorChar, exceptionByEntity.Key.Path))
                .Append("' name: '")
                .Append(exceptionByEntity.Key.FileName)
                .Append("' type '")
                .Append(exceptionByEntity.Key.ContentType)
                .Append('\'')
                .AppendLine();

            foreach (var exception in exceptionByEntity.Value.OrderBy(e => e.LineNumber))
            {
                sb
                    .Append("Line:")
                    .Append(exception.LineNumber)
                    .Append(' ')
                    .Append(exception.Text)
                    .AppendLine();
            }
        }
        
        return sb.ToString();
    }
}
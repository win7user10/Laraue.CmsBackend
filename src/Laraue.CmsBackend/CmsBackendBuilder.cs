using System.Text;

namespace Laraue.CmsBackend;

public interface ICmsBackendBuilder
{
    ICmsBackendBuilder AddContent(string markdownFileContent, DateTime updateAt);
    ICmsBackendBuilder AddContentType<TContentType>() where TContentType : ContentType;
    ICmsBackend Build();
}

public class CmsBackendBuilder(IMarkdownParser markdownParser, IMarkdownProcessor markdownProcessor)
    : ICmsBackendBuilder
{
    private readonly ContentTypeRegistry _contentTypeRegistry = new();
    private readonly ParsedMdFileRegistry _parsedMdFileRegistry = new();
    
    public ICmsBackendBuilder AddContent(string markdownFileContent, DateTime updateAt)
    {
        var result = markdownParser.Parse(markdownFileContent, updateAt);
            
        _parsedMdFileRegistry.Add(result);
        
        return this;
    }

    public ICmsBackendBuilder AddContentType<TContentType>() where TContentType : ContentType
    {
        _contentTypeRegistry.AddContentType<TContentType>();
        
        return this;
    }

    public ICmsBackend Build()
    {
        var buildResult = markdownProcessor.ApplyRegistrySchemas(
            _parsedMdFileRegistry.Values,
            _contentTypeRegistry);

        if (buildResult.Success)
        {
            return new CmsBackend(buildResult.MarkdownFiles);
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
                .Append("Entity '")
                .Append(exceptionByEntity.Key.ContentType)
                .Append(':')
                .Append(exceptionByEntity.Key.Id)
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
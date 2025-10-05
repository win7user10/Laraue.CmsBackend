namespace Laraue.CmsBackend;

public interface IMarkdownProcessor
{
    ApplyResult ApplyRegistrySchemas(IEnumerable<ParsedMdFile> mdFiles, ContentTypeRegistry typesRegistry);
}

public class MarkdownProcessor : IMarkdownProcessor
{
    public ApplyResult ApplyRegistrySchemas(IEnumerable<ParsedMdFile> mdFiles, ContentTypeRegistry typesRegistry)
    {
        var result = new ProcessedMdFileRegistry();
        var errors = new ErrorRegistry();
        
        foreach (var mdFile in mdFiles)
        {
            if (!typesRegistry.TryGetContentType(mdFile.ContentType, out var contentType))
            {
                errors.Add(mdFile, $"Content type '{mdFile.ContentType}' is not defined", 0);
                continue;
            }

            // properties should exist
            var propertyTypeByName = contentType.Properties
                .ToDictionary(x => x.Name);

            // keys that defined in md, but not defined in a model
            var unknowKeys = mdFile.Properties
                .Select(x => new { x.Name, x.SourceLineNumber })
                .ExceptBy(propertyTypeByName.Keys, arg => arg.Name);
            
            foreach (var unknowKey in unknowKeys)
            {
                errors.Add(mdFile, $"Unknown key '{unknowKey.Name}'", unknowKey.SourceLineNumber);
            }
            
            var mdFileProperties = mdFile.Properties
                .ToDictionary(x => x.Name);

            var processedFileProperties = new List<ProcessedMdFileProperty>();
            
            foreach (var property in contentType.Properties)
            {
                if (!mdFileProperties.TryGetValue(property.Name, out var mdFileProperty))
                {
                    if (property.Required)
                    {
                        errors.Add(mdFile, $"Required property '{property.Name}' is not defined", 0);
                    }
                    
                    continue;
                }

                var value = Parse(mdFileProperty.Value, property.Type);
                if (value == null)
                {
                    errors.Add(mdFile, $"Invalid case of property '{property.Name}' with value '{mdFileProperty.Value}' to type '{property.Type}'", mdFileProperty.SourceLineNumber);
                    continue;
                }
                
                processedFileProperties.Add(new ProcessedMdFileProperty { Name = property.Name, Value = value });
            }

            result.TryAdd(new ProcessedMdFile
            {
                ContentType = mdFile.ContentType,
                Id = mdFile.Id,
                Properties = processedFileProperties.ToArray(),
                UpdatedAt = mdFile.UpdatedAt,
                Content = mdFile.Content,
            });
        }

        return new ApplyResult
        {
            MarkdownFiles = result,
            Errors = errors.Errors
        };
    }

    private static object? Parse(
        string value,
        ContentTypePropertyType type)
    {
        switch (type)
        {
            case ContentTypePropertyType.String:
                return value;
            case ContentTypePropertyType.Number:
                return int.TryParse(value, out var intValue) ? intValue : null;
            case ContentTypePropertyType.DateTime:
                return DateTime.TryParse(value, out var dateTime) ? dateTime : null;
            case ContentTypePropertyType.Float:
                return double.TryParse(value, out var doubleValue) ? doubleValue : null;
            default:
                throw new InvalidOperationException();
        }
    }

    private class ErrorRegistry
    {
        public readonly Dictionary<ParsedMdFile, ICollection<ValidationError>> Errors = new ();

        public void Add(ParsedMdFile mdFile, string error, int lineNumber)
        {
            if (!Errors.TryGetValue(mdFile, out var value))
            {
                value = new List<ValidationError>();
                Errors[mdFile] = value;
            }

            value.Add(new ValidationError { Text = error, LineNumber = lineNumber });
        }
    }
}

public sealed record ApplyResult
{
    public bool Success => Errors.Count == 0;
    public required ProcessedMdFileRegistry MarkdownFiles { get; init; }
    public required IDictionary<ParsedMdFile, ICollection<ValidationError>> Errors { get; init; }
}

public sealed record ValidationError
{
    public required string Text { get; set; }
    public required int LineNumber { get; set; }
}

public sealed record ProcessedMdFile
{
    public required string Id { get; init; }
    public required string Content { get; init; }
    public required string ContentType { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public required ProcessedMdFileProperty[] Properties { get; init; }
}

public sealed record ProcessedMdFileProperty
{
    public required string Name { get; init; }
    public required object Value { get; init; }
}
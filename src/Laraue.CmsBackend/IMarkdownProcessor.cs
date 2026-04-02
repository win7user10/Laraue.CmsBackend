using Laraue.CmsBackend.Contracts;

namespace Laraue.CmsBackend;

public interface IMarkdownProcessor
{
    ApplyResult ApplyRegistrySchemas(
        IEnumerable<ParsedMdFile> mdFiles,
        ContentTypeRegistry typesRegistry);
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
                    if (property.IsRequired)
                    {
                        errors.Add(mdFile, $"Required property '{property.Name}' is not defined", 0);
                    }
                    
                    continue;
                }

                var value = Parse(mdFileProperty.Value, property.Type, property.IsArray);
                if (value == null)
                {
                    var arrayString = property.IsArray ? "[]" : string.Empty;
                    errors.Add(mdFile, $"Invalid cast of property '{property.Name}' with value '{mdFileProperty.Value}' to type '{property.Type}{arrayString}'", mdFileProperty.SourceLineNumber);
                    continue;
                }
                
                processedFileProperties.Add(new ProcessedMdFileProperty { Name = property.Name, Value = value });
            }

            var processedFile = new ProcessedMdFile(mdFile);

            foreach (var property in processedFileProperties)
            {
                processedFile.Add(property.Name, property.Value);
            }

            if (!result.TryAdd(processedFile))
            {
                errors.Add(mdFile, $"The same file already has been registered '{mdFile.LogicalPath}'", 0);
            }
        }

        return new ApplyResult
        {
            MarkdownFiles = result,
            Errors = errors.Errors
        };
    }

    private static object? Parse(
        object? value,
        ContentTypePropertyType type,
        bool isArray)
    {
        if (value == null)
        {
            return null;
        }
        
        if (isArray)
        {
            if (value is not object[] arrayParts)
            {
                throw new InvalidOperationException();
            }
            
            var result = Array.CreateInstance(GetClrType(type), arrayParts.Length);
            var hasNull = false;
            
            for (var index = 0; index < arrayParts.Length; index++)
            {
                var item = arrayParts[index];
                var parsedValue = Parse(item, type, false);
                if (parsedValue is null)
                {
                    hasNull = true;
                    break;
                }
                
                result.SetValue(parsedValue, index);
            }
            
            return hasNull ? null : result;
        }

        if (value is not string stringValue)
        {
            throw new InvalidOperationException();
        }
        
        switch (type)
        {
            case ContentTypePropertyType.String:
                return value;
            case ContentTypePropertyType.Number:
                return int.TryParse(stringValue, out var intValue) ? intValue : null;
            case ContentTypePropertyType.DateTime:
                return DateTime.TryParse(stringValue, out var dateTime) ? dateTime : null;
            case ContentTypePropertyType.Float:
                return double.TryParse(stringValue, out var doubleValue) ? doubleValue : null;
            default:
                throw new InvalidOperationException();
        }
    }

    private static Type GetClrType(ContentTypePropertyType type)
    {
        return type switch
        {
            ContentTypePropertyType.String => typeof(string),
            ContentTypePropertyType.Number => typeof(int),
            ContentTypePropertyType.DateTime => typeof(DateTime),
            ContentTypePropertyType.Float => typeof(double),
            _ => throw new NotSupportedException()
        };
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

public sealed record ProcessedMdFileProperty
{
    public required string Name { get; init; }
    public required object Value { get; init; }
}
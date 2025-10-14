using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Laraue.CmsBackend;

public class ContentTypeRegistry
{
    private readonly Dictionary<string, ContentTypeSchema> _contentTypes = new ();
    public const string UndefinedContentType = "undefined";
    
    public ContentTypeRegistry AddContentType<TContentType>() where TContentType : BaseDocumentType
    {
        var schema = GetSchema<TContentType>();

        if (!_contentTypes.TryAdd(schema.Name, schema))
        {
            throw new InvalidOperationException($"ContentType {schema.Name} has already been added");
        }
        
        return this;
    }

    public bool TryGetContentType(string id, [NotNullWhen(true)] out ContentTypeSchema? contentType)
    {
        return _contentTypes.TryGetValue(id, out contentType);
    }

    private ContentTypeSchema GetSchema<TContentType>() where TContentType : BaseDocumentType
    {
        var type = typeof(TContentType);

        var documentType = type.GetCustomAttribute<DocumentTypeAttribute>();
        var contentType = documentType != null
            ? documentType.ContentType
            : type.Name.ToCamelCase().ToString();
        
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var schemaProperties = properties.Select(GetPropertySchema).ToArray();

        return new ContentTypeSchema
        {
            Name = contentType,
            Properties = schemaProperties,
        };
    }

    private ContentTypeSchemaProperty GetPropertySchema(PropertyInfo property)
    {
        var data = new ContentTypeSchemaProperty()
        {
            IsArray = property.PropertyType.IsArray,
            Name = property.Name.ToCamelCase().ToString(),
            Type = GetPropertyType(property.PropertyType.IsArray ? property.PropertyType.GetElementType()! : property.PropertyType),
            IsRequired = Attribute.IsDefined(property, typeof(RequiredMemberAttribute))
        };

        return data;
    }

    private ContentTypePropertyType GetPropertyType(Type type)
    {
        if (type == typeof(string))
        {
            return ContentTypePropertyType.String;
        }

        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
        {
            return ContentTypePropertyType.Float;
        }
        
        if (type == typeof(DateTime))
        {
            return ContentTypePropertyType.DateTime;
        }
        
        if (type == typeof(int))
        {
            return ContentTypePropertyType.Number;
        }
        
        throw new NotSupportedException($"Property {type} is not supported");
    }

    public record ContentTypeSchema
    {
        public required string Name { get; init; }
        public required ContentTypeSchemaProperty[] Properties { get; init; }
    }

    public record ContentTypeSchemaProperty
    {
        public required string Name { get; init; }
        public required bool IsArray { get; init; }
        public required bool IsRequired { get; init; }
        public required ContentTypePropertyType Type { get; init; }
    }
    
    public enum ContentTypePropertyType
    {
        String,
        Number,
        DateTime,
        Float,
    }
}
namespace Laraue.CmsBackend.Contracts;

public enum ContentTypePropertyType
{
    String,
    Number,
    DateTime,
    Float,
}

public static class ContentTypePropertyTypeExtensions
{
    public static ContentTypePropertyType GetCmsPropertyType(this Type type)
    {
        var nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType != null)
        {
            type = nullableType;
        }
        
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
}
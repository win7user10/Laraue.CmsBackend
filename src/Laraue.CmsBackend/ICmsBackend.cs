using Laraue.Core.Exceptions.Web;

namespace Laraue.CmsBackend;

public interface ICmsBackend
{
    Dictionary<string, object> GetEntity(GetEntityRequest request);
    IEnumerable<Dictionary<string, object>> GetEntities(GetEntitiesRequest request);
}

public class GetEntityRequest
{
    public required MdFileKey Key { get; set; }
    public string[]? Properties { get; set; }
}

public class GetEntitiesRequest
{
    public string[]? Properties { get; set; }
}

public class CmsBackend(ProcessedMdFileRegistry registry) : ICmsBackend
{
    public Dictionary<string, object> GetEntity(GetEntityRequest request)
    {
        return registry.TryGet(request.Key, out var value)
            ? MapMdFileToDto(value, request.Properties)
            : throw new NotFoundException();
    }

    public IEnumerable<Dictionary<string, object>> GetEntities(GetEntitiesRequest request)
    {
        return registry.GetEntities().Select(entity => MapMdFileToDto(entity, request.Properties));
    }

    private Dictionary<string, object> MapMdFileToDto(ProcessedMdFile mdFile, string[]? includeProperties)
    {
        var result = new Dictionary<string, object>();

        AddIfInIncludeProperties("id", mdFile.Id);
        AddIfInIncludeProperties("type", mdFile.ContentType);
        AddIfInIncludeProperties("updatedAt", mdFile.UpdatedAt);
        AddIfInIncludeProperties("content", mdFile.Content);

        foreach (var property in mdFile.Properties)
        {
            AddIfInIncludeProperties(property.Name, property.Value);
        }
        
        return result;

        void AddIfInIncludeProperties(string propertyName, object value)
        {
            if (includeProperties is null || includeProperties.Contains(propertyName))
            {
                result.Add(propertyName, value);
            }
        }
    }
}
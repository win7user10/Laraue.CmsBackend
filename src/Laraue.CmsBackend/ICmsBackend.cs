using Laraue.Core.Exceptions.Web;

namespace Laraue.CmsBackend;

public interface ICmsBackend
{
    Dictionary<string, object> GetEntity(MdFileKey key);
}

public class CmsBackend(ProcessedMdFileRegistry registry) : ICmsBackend
{
    public Dictionary<string, object> GetEntity(MdFileKey key)
    {
        return registry.TryGet(key, out var value) ? MapMdFileToDto(value) : throw new NotFoundException();
    }

    private Dictionary<string, object> MapMdFileToDto(ProcessedMdFile mdFile)
    {
        var result = new Dictionary<string, object>();
        
        result.Add("id", mdFile.Id);
        result.Add("type", mdFile.ContentType);
        result.Add("updatedAt", mdFile.UpdatedAt);
        result.Add("content", mdFile.Content);

        foreach (var property in mdFile.Properties)
        {
            result.Add(property.Name, property.Value);
        }
        
        return result;
    }
}
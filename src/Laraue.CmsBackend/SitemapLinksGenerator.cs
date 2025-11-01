using Laraue.Core.DataAccess.Contracts;

namespace Laraue.CmsBackend;

public interface ISitemapGenerator
{
    SitemapItemDto[] GetItems();
}

public class SitemapGenerator(ICmsBackend cmsBackend) : ISitemapGenerator
{
    public SitemapItemDto[] GetItems()
    {
        var entities = cmsBackend.GetEntities<GetEntitiesResult>(new GetEntitiesRequest
        {
            Pagination = new PaginationData { Page = 0, PerPage = int.MaxValue - 1 },
            Properties = ["updatedAt", "fileName", "path"]
        });
        
        var result = new List<SitemapItemDto>();
        foreach (var entity in entities.Data)
        {
            var location = string.Join("/", entity.Path.Append(entity.FileName));
            result.Add(new SitemapItemDto(location, entity.UpdatedAt));
        }
        
        return result.ToArray();
    }
}

public record GetEntitiesResult
{
    public DateTime UpdatedAt { get; init; }
    public required string FileName { get; init; }
    public required string[] Path { get; init; }
}

public record SitemapItemDto(string Loc, DateTime LastMod);
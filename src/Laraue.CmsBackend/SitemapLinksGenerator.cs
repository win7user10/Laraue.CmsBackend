using System.Text;
using System.Xml;
using Laraue.Core.DataAccess.Contracts;

namespace Laraue.CmsBackend;

public interface ISitemapGenerator
{
    SitemapItemDto[] GetItems();
    string GenerateSitemap(GenerateSitemapRequest request, SitemapItemDto[] items);
}

public class GenerateSitemapRequest
{
    public string? BaseAddress { get; init; }
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

    public string GenerateSitemap(GenerateSitemapRequest request, SitemapItemDto[] items)
    {
        var sb = new StringBuilder();

        sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        
        foreach (var item in items)
        {
            sb
                .Append("<url>")
                .Append("<loc>");

            if (request.BaseAddress != null)
                sb.Append(request.BaseAddress);
            
            sb
                .Append(item.Location)
                .Append("</loc>")
                .Append("<lastmod>")
                .Append(XmlConvert.ToString(item.LastModified, XmlDateTimeSerializationMode.Utc))
                .Append("</lastmod>")
                .Append("</url>");
        }
        
        sb.Append("</urlset>");

        return sb.ToString();
    }
}

public record GetEntitiesResult
{
    public DateTime UpdatedAt { get; init; }
    public required string FileName { get; init; }
    public required string[] Path { get; init; }
}

public record SitemapItemDto(string Location, DateTime LastModified);
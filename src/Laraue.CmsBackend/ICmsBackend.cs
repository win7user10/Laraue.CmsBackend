using System.Text.Json.Serialization;
using Laraue.CmsBackend.Contracts;
using Laraue.Core.DataAccess.Contracts;
using Laraue.Core.DataAccess.Extensions;
using Laraue.Core.Exceptions.Web;

namespace Laraue.CmsBackend;

public interface ICmsBackend
{
    Dictionary<string, object> GetEntity(GetEntityRequest request);
    IShortPaginatedResult<Dictionary<string, object>> GetEntities(GetEntitiesRequest request);
    List<CountPropertyRow> CountPropertyValues(CountPropertyValuesRequest request);
    List<SectionItem> GetSections(GetSectionsRequest request);
}

public class GetSectionsRequest
{
    public string? FromPath { get; init; }
    public required int Depth { get; init; }
}

public class SectionItem
{
    public required string RelativePath { get; init; }
    public required string FullPath { get; init; }
    public required SectionItem[] Children { get; init; }
    public required bool HasContent { get; init; }
}

public class GetEntitiesRequest : IPaginatedRequest
{
    public string[]? Properties { get; set; }
    public FilterRow[]? Filters { get; set; }
    public SortRow[]? Sorting { get; set; }
    public required PaginationData Pagination { get; init; }
}

public class GetEntityRequest
{
    public required MdFileKey Key { get; set; }
    public string[]? Properties { get; set; }
}

public class CountPropertyValuesRequest
{
    public required string Property { get; init; }
    public FilterRow[]? Filters { get; init; }
}

public class CountPropertyRow
{
    public required object Key { get; init; }
    public required int Count { get; init; }
}

public class FilterRow
{
    public required string Property { get; set; }
    
    [JsonConverter(typeof(ObjectConverter))]
    public required object Value { get; set; }
    public required FilterOperator Operator { get; set; }
}

public enum FilterOperator
{
    Equals,
}

public class SortRow
{
    public required string Property { get; set; }
    public SortOrder SortOrder { get; set; }
}

public enum SortOrder
{
    Ascending,
    Descending,
}

public class CmsBackend(ProcessedMdFileRegistry registry) : ICmsBackend
{
    public Dictionary<string, object> GetEntity(GetEntityRequest request)
    {
        return registry.TryGet(request.Key, out var value)
            ? MapMdFileToDto(value, request.Properties)
            : throw new NotFoundException();
    }

    public IShortPaginatedResult<Dictionary<string, object>> GetEntities(GetEntitiesRequest request)
    {
        var source = registry.GetEntities();
        var filteredSource = ApplyFilters(source, request.Filters);
        var orderedSource = ApplySort(filteredSource, request.Sorting);
        var projectedSource = ApplyMap(orderedSource, request.Properties);
        
        return projectedSource.ShortPaginate(request);
    }

    public List<CountPropertyRow> CountPropertyValues(CountPropertyValuesRequest request)
    {
        var source = registry.GetEntities();
        var filteredSource = ApplyFilters(source, request.Filters);
        var projectedSource = ApplyMap(filteredSource, [request.Property]);

        var flatSource = projectedSource
            .SelectMany(x =>
            {
                if (!x.TryGetValue(request.Property, out var value))
                {
                    return [];
                }
                
                return value as object[] ?? [value];
            });
        
        return flatSource
            .GroupBy(x => x)
            .Select(x => new CountPropertyRow
            {
                Count = x.Count(),
                Key = x.Key
            })
            .ToList();
    }

    public List<SectionItem> GetSections(GetSectionsRequest request)
    {
        var subSections = registry.GetSubSections(request.FromPath, request.Depth);

        return subSections.Select(Map).ToList();
    }

    private SectionItem Map(ProcessedMdFileRegistry.SubSectionItem sectionItem)
    {
        return new SectionItem
        {
            RelativePath = sectionItem.RelativePath,
            FullPath = sectionItem.FullPath,
            Children = sectionItem.Children.Select(Map).ToArray(),
            HasContent = sectionItem.HasContent,
        };
    }

    private IEnumerable<ProcessedMdFile> ApplyFilters(
        IEnumerable<ProcessedMdFile> source,
        FilterRow[]? filters)
    {
        if (filters == null || filters.Length == 0)
        {
            return source;
        }

        return source.Where(i => MatchFilters(i, filters));
    }
    
    private IEnumerable<ProcessedMdFile> ApplySort(
        IEnumerable<ProcessedMdFile> source,
        SortRow[]? sorting)
    {
        if (sorting == null || sorting.Length == 0)
        {
            return source;
        }

        var firstSorting = sorting[0];
        var orderedSource = firstSorting.SortOrder == SortOrder.Descending
            ? source.OrderByDescending(r => r[firstSorting.Property])
            : source.OrderBy(r => r[firstSorting.Property]);

        if (sorting.Length == 1)
        {
            return orderedSource;
        }
        
        foreach (var sortRow in sorting.Skip(1))
        {
            orderedSource = sortRow.SortOrder == SortOrder.Descending
                ? orderedSource.ThenByDescending(r => r[sortRow.Property])
                : orderedSource.ThenBy(r => r[sortRow.Property]);
        }
        
        return orderedSource;
    }

    private bool MatchFilters(ProcessedMdFile item, FilterRow[] filters)
    {
        foreach (var filter in filters)
        {
            if (!MatchFilter(item, filter))
            {
                return false;
            }
        }
        
        return true;
    }

    private bool MatchFilter(ProcessedMdFile item, FilterRow filter)
    {
        if (!item.TryGetValue(filter.Property, out var value))
        {
            return true;
        }

        return filter.Operator switch
        {
            FilterOperator.Equals => filter.Value.Equals(value),
            _ => throw new NotImplementedException()
        };
    }
    
    private IEnumerable<Dictionary<string, object>> ApplyMap(IEnumerable<ProcessedMdFile> mdFiles, string[]? includeProperties)
    {
        return mdFiles.Select(x => MapMdFileToDto(x, includeProperties));
    }

    private static Dictionary<string, object> MapMdFileToDto(ProcessedMdFile mdFile, string[]? includeProperties)
    {
        if (includeProperties is null)
        {
            return mdFile;
        }
        
        return includeProperties
            .Where(mdFile.ContainsKey)
            .ToDictionary(propertyName => propertyName, propertyName => mdFile[propertyName]);
    }
}
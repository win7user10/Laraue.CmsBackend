using Laraue.Core.DataAccess.Contracts;
using Laraue.Core.DataAccess.Extensions;
using Laraue.Core.Exceptions.Web;

namespace Laraue.CmsBackend;

public interface ICmsBackend
{
    Dictionary<string, object> GetEntity(GetEntityRequest request);
    IShortPaginatedResult<Dictionary<string, object>> GetEntities(GetEntitiesRequest request);
}

public class GetEntityRequest
{
    public required MdFileKey Key { get; set; }
    public string[]? Properties { get; set; }
}

public class GetEntitiesRequest : IPaginatedRequest
{
    public string[]? Properties { get; set; }
    public FilterRow[]? Filters { get; set; }
    public SortRow[]? Sorting { get; set; }
    public required PaginationData Pagination { get; init; }
}

public class FilterRow
{
    public required string Property { get; set; }
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
        var projectedSource = orderedSource.Select(x => MapMdFileToDto(x, request.Properties));
        
        return projectedSource.ShortPaginate(request);
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
            FilterOperator.Equals => value == filter.Value,
            _ => throw new NotImplementedException()
        };
    }

    private Dictionary<string, object> MapMdFileToDto(ProcessedMdFile mdFile, string[]? includeProperties)
    {
        return includeProperties is null
            ? mdFile
            : includeProperties.ToDictionary(property => property, property => mdFile[property]);
    }
}
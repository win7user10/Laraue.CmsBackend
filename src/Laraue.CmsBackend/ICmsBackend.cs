using System.Collections;
using System.Text.Json.Serialization;
using Laraue.CmsBackend.Contracts;
using Laraue.CmsBackend.Funtions;
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
    public string[]? FromPath { get; init; }
    public required int Depth { get; init; }
}

public class GetEntitiesRequest : IPaginatedRequest
{
    public string[]? FromPath { get; init; }
    public string[]? Properties { get; set; }
    public FilterRow[]? Filters { get; set; }
    public SortRow[]? Sorting { get; set; }
    public required PaginationData Pagination { get; init; }
}

public class GetEntityRequest
{
    public required string[] Path { get; set; }
    public string[]? Properties { get; set; }
}

public class CountPropertyValuesRequest
{
    public required string Property { get; init; }
    public string[]? FromPath { get; set; }
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
    More,
    Less,
    MoreOrEqual,
    LessOrEqual,
    In,
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

public class CmsBackendUnit(
    ProcessedMdFileRegistry registry,
    CmsBackendFunctionsRegistry functionsRegistry)
    : ICmsBackend
{
    public Dictionary<string, object> GetEntity(GetEntityRequest request)
    {
        return registry.TryGet(request.Path, out var value)
            ? MapMdFileToDto(value, request.Properties)
            : throw new NotFoundException();
    }

    public IShortPaginatedResult<Dictionary<string, object>> GetEntities(GetEntitiesRequest request)
    {
        var source = registry.GetEntities(request.FromPath);
        var filteredSource = ApplyFilters(source, request.Filters);
        var orderedSource = ApplySort(filteredSource, request.Sorting);
        var projectedSource = ApplyMap(orderedSource, request.Properties);
        
        return projectedSource.ShortPaginate(request);
    }

    public List<CountPropertyRow> CountPropertyValues(CountPropertyValuesRequest request)
    {
        var source = registry.GetEntities(request.FromPath);
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
        var subSections = registry.GetSubSections(request.FromPath ?? [], request.Depth);

        return subSections.Select(Map).ToList();
    }

    private SectionItem Map(ProcessedMdFileRegistry.SubSectionItem sectionItem)
    {
        return new SectionItem
        {
            FileName = sectionItem.FileName,
            FullPath = sectionItem.FullPath,
            RelativePath = sectionItem.RelativePath,
            Children = sectionItem.Children.Select(Map).ToArray(),
            HasContent = sectionItem.HasContent,
            Title = sectionItem.Title,
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
        var property = filter.Property;

        var parameters = PropertyParser.ParsePropertyParameters(filter.Property);
        property = parameters.FunctionParameters?.CalleeName ?? property;
        
        if (!item.TryGetValue(property, out var value))
        {
            return false;
        }

        if (parameters.FunctionParameters is not null)
        {
            if (!functionsRegistry.TryExecuteDelegate(parameters.FunctionParameters, value, out var newValue))
            {
                throw new InvalidMethodException($"There is no registered function that can handle '{filter.Property}'.");
            }

            value = newValue;
        }

        return filter.Operator switch
        {
            FilterOperator.Equals => value.Equals(filter.Value),
            FilterOperator.More => value is IComparable comparable && comparable.CompareTo(filter.Value) > 0,
            FilterOperator.MoreOrEqual => value is IComparable comparable && comparable.CompareTo(filter.Value) >= 0,
            FilterOperator.Less => value is IComparable comparable && comparable.CompareTo(filter.Value) < 0,
            FilterOperator.LessOrEqual => value is IComparable comparable && comparable.CompareTo(filter.Value) <= 0,
            FilterOperator.In => value is IEnumerable enumerable && enumerable.Cast<object>().Contains(filter.Value),
            _ => throw new NotImplementedException()
        };
    }
    
    private IEnumerable<Dictionary<string, object>> ApplyMap(IEnumerable<ProcessedMdFile> mdFiles, string[]? includeProperties)
    {
        return mdFiles.Select(x => MapMdFileToDto(x, includeProperties));
    }
    
    private Dictionary<string, object> MapMdFileToDto(ProcessedMdFile mdFile, string[]? includeProperties)
    {
        if (includeProperties is null)
        {
            return mdFile;
        }
        
        var result = new Dictionary<string, object>();
        foreach (var includeProperty in includeProperties)
        {
            var requestedProperty = includeProperty;
            string? alias = null;
            
            var propertyParameters = PropertyParser.ParsePropertyParameters(includeProperty);
            if (propertyParameters.Alias is not null)
            {
                alias = propertyParameters.Alias;
            }
            if (propertyParameters.FunctionParameters is not null)
            {
                requestedProperty = propertyParameters.FunctionParameters.CalleeName;
            }
            if (propertyParameters.FunctionParameters is null)
            {
                requestedProperty = propertyParameters.PropertyName ?? throw new InvalidOperationException();
            }

            // TODO - compile the method only once.
            if (mdFile.TryGetValue(requestedProperty, out var value))
            {
                if (propertyParameters.FunctionParameters is null)
                {
                    result[alias ?? requestedProperty] = value;
                }
                else
                {
                    if (!functionsRegistry.TryExecuteDelegate(propertyParameters.FunctionParameters, value, out var newValue))
                    {
                        throw new InvalidMethodException($"There is no registered function that can handle '{includeProperty}'.");
                    }

                    result[alias ?? propertyParameters.FunctionParameters.FunctionName] = newValue;
                }
            }
        }

        return result;
    }
}
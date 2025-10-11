using Laraue.CmsBackend.Contracts;
using Laraue.Core.DataAccess.Contracts;
using Laraue.Core.Exceptions.Web;

namespace Laraue.CmsBackend.UnitTests;

public class CmsBackendTests
{
    private readonly ICmsBackend _cmsBackend;
    
    public CmsBackendTests()
    {
        var files = new ProcessedMdFileRegistry();

        files.TryAdd(new ProcessedMdFile(new Dictionary<string, object>{
            ["contentType"] = "article",
            ["content"] = "hi",
            ["id"] = "introduction",
            ["updatedAt"] = new DateTime(2020, 01, 01),
            ["project"] = "project1",
            ["tags"] = new [] { "tag1", "tag2" },
        }));
        
        
        files.TryAdd(new ProcessedMdFile(new Dictionary<string, object>{
            ["contentType"] = "article",
            ["content"] = "bye",
            ["id"] = "parting",
            ["updatedAt"] = new DateTime(2020, 01, 02),
            ["project"] = "project2",
            ["tags"] = new [] { "tag2", "tag3" },
        }));
        
        _cmsBackend = new CmsBackend(files);
    }
    
    [Fact]
    public void GetEntity_ShouldThrows_WhenEntityIsNotExists()
    {
        Assert.Throws<NotFoundException>(() => _cmsBackend.GetEntity(new GetEntityRequest
        {
            Key = new MdFileKey { Id = "1", ContentType = "1" }
        }));
    }
    
    [Fact]
    public void GetEntity_ShouldReturnsOnlyRequestedField_WhenRequestHasListOfRequiredProperties()
    {
        var result = _cmsBackend.GetEntity(new GetEntityRequest
        {
            Key = new MdFileKey { Id = "introduction", ContentType = "article" },
            Properties = ["project"]
        });
        
        var property = Assert.Single(result);
        Assert.Equal("project1", property.Value);
        Assert.Equal("project", property.Key);
    }
    
    [Fact]
    public void GetEntities_ShouldReturnsOnlyRequestedField_WhenRequestHasListOfRequiredProperties()
    {
        var result = _cmsBackend.GetEntities(new GetEntitiesRequest
        {
            Properties = ["project"],
            Pagination = GetDefaultPagination()
        });
        
        Assert.Equal(2, result.Data.Count);
        
        var property = Assert.Single(result.Data[0]);
        Assert.Equal("project1", property.Value);
        Assert.Equal("project", property.Key);
    }
    
    [Fact]
    public void GetEntities_ShouldReturnsOnlyFilteredEntities_WhenFilterIsPassed()
    {
        var result = _cmsBackend.GetEntities(new GetEntitiesRequest
        {
            Filters =
            [
                new FilterRow { Property = "id", Value = "introduction", Operator = FilterOperator.Equals }
            ],
            Pagination = GetDefaultPagination()
        });
        
        Assert.Single(result.Data);
    }
    
    [Theory]
    [InlineData(SortOrder.Ascending, 2)]
    [InlineData(SortOrder.Descending, 1)]
    public void GetEntities_ShouldBeSorted_WhenSortingIsPassed(SortOrder sortOrder, int exceptedSecondEntityDay)
    {
        var result = _cmsBackend.GetEntities(new GetEntitiesRequest
        {
            Sorting = 
            [
                new SortRow { Property = "updatedAt", SortOrder = sortOrder }
            ],
            Pagination = new PaginationData { Page = 1, PerPage = 1 }
        });

        var creationDate = result
            .Data
            .Select(i => (DateTime)i["updatedAt"])
            .Single();
        
        Assert.Equal(new DateTime(2020, 01, exceptedSecondEntityDay), creationDate);
    }
    
    [Fact]
    public void CountPropertyValues_ShouldReturnsCount_OnArrayValues()
    {
        var result = _cmsBackend.CountPropertyValues(new CountPropertyValuesRequest
        {
            Property = "tags"
        });
        
        Assert.Equal(3, result.Count);

        var resultDictionary = result.ToDictionary(x => x.Key, x => x.Count);
        Assert.Equal(1, resultDictionary["tag1"]);
        Assert.Equal(2, resultDictionary["tag2"]);
        Assert.Equal(1, resultDictionary["tag3"]);
    }

    private static PaginationData GetDefaultPagination()
    {
        return new PaginationData
        {
            Page = 0,
            PerPage = 10,
        };
    }
}
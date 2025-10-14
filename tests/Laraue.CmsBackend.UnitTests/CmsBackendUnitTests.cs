using Laraue.CmsBackend.Contracts;
using Laraue.CmsBackend.MarkdownTransformation;
using Laraue.Core.DataAccess.Contracts;
using Laraue.Core.Exceptions.Web;

namespace Laraue.CmsBackend.UnitTests;

public class CmsBackendUnitTests
{
    private readonly ICmsBackend _cmsBackend;

    private class UnitTestArticle : BaseDocumentType
    {
        public required string[] Tags { get; set; }
        public required string Project { get; set; }
    }
    
    public CmsBackendUnitTests()
    {
        var content1Text = @"---
tags: [tag1, tag2]
project: project1
type: unitTestArticle
---
hi";
        var content1 =  new ContentProperties(
            content1Text,
            new FilePath(["docs", "articles"]),
            "article1",
            new DateTime(2020, 01, 01),
            new DateTime(2020, 01, 01));
        
        var content2Text = @"---
tags: [tag2, tag3]
project: project2
type: unitTestArticle
---
hi";
        
        var content2 = new ContentProperties(
            content2Text,
            new FilePath(["docs", "articles"]),
            "article2",
            new DateTime(2020, 01, 01),
            new DateTime(2020, 01, 02));
        
        _cmsBackend = new CmsBackendBuilder(
                new MarkdownParser(
                    new MarkdownToHtmlTransformer(),
                    new ArticleInnerLinksGenerator()),
                new MarkdownProcessor())
            .AddContentType<UnitTestArticle>()
            .AddContent(content1)
            .AddContent(content2)
            .Build();
    }
    
    [Fact]
    public void GetEntity_ShouldThrows_WhenEntityIsNotExists()
    {
        Assert.Throws<NotFoundException>(() => _cmsBackend.GetEntity(new GetEntityRequest
        {
            Path = ["1", "1"]
        }));
    }
    
    [Fact]
    public void GetEntity_ShouldReturnsOnlyRequestedField_WhenRequestHasListOfRequiredProperties()
    {
        var result = _cmsBackend.GetEntity(new GetEntityRequest
        {
            Path = ["docs", "articles", "article1"],
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
    public void GetEntities_ShouldReturnsOnlyFilteredEntities_WhenComplexFilterIsPassed()
    {
        var result = _cmsBackend.GetEntities(new GetEntitiesRequest
        {
            Filters =
            [
                new FilterRow { Property = "fileName", Value = "article2", Operator = FilterOperator.Equals },
                new FilterRow { Property = "project", Value = "project2", Operator = FilterOperator.Equals },
            ],
            Pagination = GetDefaultPagination()
        });
        
        Assert.Single(result.Data);
    }
    
    [Fact]
    public void GetEntities_ShouldReturnsOnlyFilteredEntities_WhenByContentTypePassed()
    {
        var result = _cmsBackend.GetEntities(new GetEntitiesRequest
        {
            Filters =
            [
                new FilterRow { Property = "contentType", Value = "unitTestArticle", Operator = FilterOperator.Equals }
            ],
            Pagination = GetDefaultPagination()
        });
        
        Assert.Equal(2, result.Data.Count);
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
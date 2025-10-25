using Laraue.CmsBackend.Contracts;
using Laraue.CmsBackend.MarkdownTransformation;

namespace Laraue.CmsBackend;

public interface IArticleInnerLinksGenerator
{
    ICollection<ArticleInnerLink> ParseLinks(MarkdownTree markdownExpression);
}

public class ArticleInnerLinksGenerator : IArticleInnerLinksGenerator
{
    public ICollection<ArticleInnerLink> ParseLinks(MarkdownTree markdownExpression)
    {
        var result = new List<ArticleInnerLink>();
        
        foreach (var headingBlock in markdownExpression.Content.OfType<HeadingBlock>())
        {
            
        }
        
        return result;
    }
}
using System.Text;
using Laraue.CmsBackend.Contracts;
using Laraue.CmsBackend.MarkdownTransformation;
using Laraue.CmsBackend.Utils;

namespace Laraue.CmsBackend;

public interface IArticleInnerLinksGenerator
{
    ICollection<ArticleInnerLink> ParseLinks(MarkdownTree markdownExpression);
}

public class ArticleInnerLinksGenerator : IArticleInnerLinksGenerator
{
    public ICollection<ArticleInnerLink> ParseLinks(MarkdownTree markdownExpression)
    {
        var allLinks = new List<ArticleInnerLink>();
        
        foreach (var headingBlock in markdownExpression.Content.OfType<HeadingBlock>())
        {
            var sb = new StringBuilder();

            foreach (var plainElement in headingBlock.Elements.OfType<PlainElement>())
            {
                sb.Append(plainElement.Literal ?? plainElement.Lexeme);
            }

            var linkText = sb.ToString();
            var linkId = HeadingUtility.GenerateHeadingId(linkText).Insert(0, '#');
            
            allLinks.Add(new ArticleInnerLink
            {
                Link = linkId.ToString(),
                Title = linkText,
                Level = headingBlock.Level,
            });
        }
        
        return allLinks;
    }
}
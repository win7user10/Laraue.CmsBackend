using System.Text;
using Laraue.CmsBackend.Contracts;
using Laraue.CmsBackend.Utils;

namespace Laraue.CmsBackend;

public interface IArticleInnerLinksGenerator
{
    ICollection<ArticleInnerLink> ParseLinks(string markdown);
}

public class ArticleInnerLinksGenerator : IArticleInnerLinksGenerator
{
    public ICollection<ArticleInnerLink> ParseLinks(string markdown)
    {
        var stringReader = new SpanReader(markdown);
        var result = new List<ArticleInnerLink>();

        while (true)
        {
            if (!stringReader.TryReadLine(out var line))
            {
                return result;
            }

            var reader = new SpanReader(line);
            if (!HeadingUtility.TryReadHeading(ref reader, out var heading))
            {
                continue;
            }
            
            var idBuilder = HeadingUtility.GenerateHeadingId(heading.Text);
            idBuilder.Insert(0, '#');
                
            result.Add(new ArticleInnerLink
            {
                Level = heading.Level,
                Link = idBuilder.ToString(),
                Title = heading.Text.ToString()
            });
        }
    }
}
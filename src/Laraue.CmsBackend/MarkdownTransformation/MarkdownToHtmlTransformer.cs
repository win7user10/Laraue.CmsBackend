using System.Text;

namespace Laraue.CmsBackend.MarkdownTransformation;

public class MarkdownToHtmlTransformer : ITransformer
{
    private readonly MdTokenExpressionWriter _writer = new ();
    
    public string Transform(MarkdownTree expr)
    {
        var stringBuilder = new StringBuilder();
        _writer.Write(stringBuilder, expr);
        
        return stringBuilder.ToString();
    }
}
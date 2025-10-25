namespace Laraue.CmsBackend.MarkdownTransformation;

public interface ITransformer
{
    string Transform(MarkdownTree expr);
}
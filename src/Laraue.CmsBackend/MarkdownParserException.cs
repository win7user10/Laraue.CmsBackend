namespace Laraue.CmsBackend;

public class MarkdownParserException : Exception
{
    public int LineNumber { get; }
    
    public MarkdownParserException(string message, int lineNumber)
        : base(message)
    {
        LineNumber = lineNumber;
    }
}
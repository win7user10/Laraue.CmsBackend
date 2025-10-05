namespace Laraue.CmsBackend;

public class MarkdownParserException(string message, int lineNumber)
    : Exception(message)
{
    public int LineNumber { get; } = lineNumber;
}
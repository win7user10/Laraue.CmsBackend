using Laraue.Interpreter.Common;

namespace Laraue.CmsBackend;

public class MarkdownParserException : Exception
{
    public MarkdownParserException(string message, CompileException exception)
        : base(message, exception)
    {
    }
}
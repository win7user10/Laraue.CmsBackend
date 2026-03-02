using System.Text;

namespace Laraue.CmsBackend.Utils;

public static class HeadingUtility
{
    public static StringBuilder GenerateHeadingId(ReadOnlySpan<char> text)
    {
        var reader = new SpanReader(text);
        var sb = new StringBuilder();
        
        while (true)
        {
            if (!reader.TryRead(out var nextChar))
            {
                return sb;
            }
            
            if (nextChar == ' ')
            {
                sb.Append('-');
            }
            else if (char.IsUpper(nextChar))
            {
                sb.Append(char.ToLower(nextChar));
            }
            else
            {
                sb.Append(nextChar);
            }
        }
    }
}

public ref struct HeadingInfo
{
    public required int Level { get; init; }
    public required ReadOnlySpan<char> Text { get; init; }
}
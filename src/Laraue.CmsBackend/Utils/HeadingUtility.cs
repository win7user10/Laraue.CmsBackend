using System.Text;

namespace Laraue.CmsBackend.Utils;

public static class HeadingUtility
{
    public const int MaxHeadingCount = 7;
    
    public static bool TryGetHeadingLevel(string source, out int level)
    {
        level = 0;
        foreach (var _ in source.TakeWhile(sourceChar => sourceChar == '#'))
        {
            level++;

            if (level == MaxHeadingCount)
            {
                break;
            }
        }

        return level != 0;
    }
    
    public static bool TryReadHeading(ref SpanReader reader, out HeadingInfo headingInfo)
    {
        var headingCount = 0;
        
        foreach (var _ in reader.TakeWhile(sourceChar => sourceChar == '#'))
        {
            headingCount++;

            if (headingCount == MaxHeadingCount)
            {
                break;
            }
        }

        if (headingCount == 0)
        {
            headingInfo = default;
            return false;
        }

        var line = reader.ReadLine(trimEntries: true);
        headingInfo = new HeadingInfo
        {
            Level = headingCount,
            Text = line,
        };
        
        return true;
    }

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
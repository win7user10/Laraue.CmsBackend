using System.Diagnostics.CodeAnalysis;

namespace Laraue.CmsBackend.MarkdownTransformation;

public static class MdElementExtensions
{
    public static MdElement[] Trim(this MdElement[] elements)
    {
        int? firstNonWhitespaceIndex = null;
        int? lastNonWhitespaceIndex = null;

        for (var index = 0; index < elements.Length; index++)
        {
            var element = elements[index];
            if (!element.IsWhitespace())
            {
                firstNonWhitespaceIndex ??= index;
                lastNonWhitespaceIndex = index;
            }
        }

        var from = firstNonWhitespaceIndex ?? 0;
        var to = lastNonWhitespaceIndex is null ? 0 : lastNonWhitespaceIndex.Value + 1;

        return elements[from..to];
    }

    private static readonly HashSet<string> TokensToAddWhitespace = [",", "."];

    // It should be method that adjust all blocks in time and calls only on the parsing end
    public static MdElement[] Adjust(this IList<MdElement> elements)
    {
        var result = new List<MdElement>();
        
        for (var index = 0; index < elements.Count; index++)
        {
            var element = elements[index];
            
            // Add space when it is not added to keep formatting
            if (element.IsPlainElement(ParsedMdTokenType.NewLine)
                && elements.TryGetAt(index + 1, out var next)
                && next is PlainElement plainElement2
                && !result.LastOrDefault().IsPlainElement(ParsedMdTokenType.NewLine)
                && (plainElement2.TokenType == ParsedMdTokenType.Word || TokensToAddWhitespace.Contains(plainElement2.Literal)))
            {
                result.Add(new PlainElement(ParsedMdTokenType.Space));
                continue;
            }
            
            // Don't handle new line when the new line token is next
            if (element.IsPlainElement(ParsedMdTokenType.NewLine)
                && (!elements.TryGetAt(index + 1, out next)
                    || next.IsPlainElement(ParsedMdTokenType.NewLine)
                    || result.LastOrDefault().IsPlainElement(ParsedMdTokenType.NewLine)))
            {
                continue;
            }
            
            // Transform two spaces -> new line
            if (element.IsWhitespace() && elements.TryGetAt(index + 1, out next) && next.IsWhitespace())
            {
                index++;
                result.Add(new PlainElement(ParsedMdTokenType.NewLine));
                continue;
            }
            
            result.Add(element);
        }
        
        return result.ToArray();
    }

    private static bool TryGetAt<T>(this IList<T> array, int index, [NotNullWhen(true)] out T? element)
        where T : class
    {
        if (array.Count <= index)
        {
            element = null;
            return false;
        }
        
        element = array[index];
        return true;
    }
    
    public static bool IsWhitespace(this MdElement element)
    {
        return element.IsPlainElement(ParsedMdTokenType.Space);
    }
    
    public static bool IsPlainElement(this MdElement? element, ParsedMdTokenType? tokenType)
    {
        return element is PlainElement plainElement && plainElement.TokenType == tokenType;;
    }
}
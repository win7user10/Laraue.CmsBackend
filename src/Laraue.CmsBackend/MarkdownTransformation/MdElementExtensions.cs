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

    public static bool IsWhitespace(this MdElement element)
    {
        return element.IsPlainElement(MdTokenType.Whitespace);
    }
    
    public static bool IsPlainElement(this MdElement element, MdTokenType? tokenType)
    {
        return element is PlainElement plainElement && plainElement.TokenType == tokenType;;
    }
}
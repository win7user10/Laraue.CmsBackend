using System.Text;

namespace Laraue.CmsBackend;

public ref struct SpanReader(ReadOnlySpan<char> source)
{
    private int CurrentLine { get; set; }
    public ReadOnlySpan<char> Source = source;
    public bool IsEndOfFileReached => Source.Length == 0;

    public ReadOnlySpan<char> ReadLine(bool trimEntries = false)
    {
        if (!TryReadLine(out var nextLine, trimEntries))
        {
            throw new InvalidOperationException();
        }
        
        return nextLine;
    }

    public bool TryReadLine(out ReadOnlySpan<char> line, bool trimEntries = false)
    {
        if (IsEndOfFileReached)
        {
            line = default;
            return false;
        }
        
        var currentIndex = 0;
        int? lastNonWhitespaceCharacterIndex = null;
        while (true)
        {
            // End of the source
            if (!TryPeekChar(currentIndex, out var nextChar))
            {
                line = Source;
                Source = default;
                return true;
            }

            currentIndex++;

            switch (nextChar)
            {
                // End of line
                case '\n':
                    CurrentLine++;
                    line = Source[..(lastNonWhitespaceCharacterIndex ?? 0)];
                    Source = Source[(currentIndex)..];
                    return true;
                case '\r' when TryPeekChar(currentIndex, out var furtherChar) && furtherChar == '\n':
                    CurrentLine++;
                    currentIndex++;
                    line = Source[..(lastNonWhitespaceCharacterIndex ?? 0)];
                    Source = Source[(currentIndex)..];
                    return true;
                case ' ' when trimEntries && lastNonWhitespaceCharacterIndex == null:
                    Source = Source[1..];
                    continue;
            }
            
            lastNonWhitespaceCharacterIndex = currentIndex;
        }
    }

    public ReadOnlySpan<char> ReadIdentifier()
    {
        var currentIndex = 0;
        TakeWhile(char.IsWhiteSpace);
        if (TryPeekChar('"'))
        {
            return ReadString();
        }
        
        while (true)
        {
            if (!TryPeekChar(currentIndex, out var nextChar))
            {
                break;
            }

            if (!char.IsLetterOrDigit(nextChar))
            {
                break;
            }
            
            currentIndex++;
        }

        return SetNewSourceIndex(currentIndex);
    }

    public ReadOnlySpan<char> ReadString()
    {
        var currentIndex = 1;
        while (true)
        {
            if (!TryPeekChar(currentIndex, out var nextChar))
            {
                throw new InvalidOperationException("String end was not found");
            }
            
            currentIndex++;

            if (nextChar == '"')
            {
                return SetNewSourceIndex(currentIndex);
            }
        }
    }
    
    public ReadOnlySpan<char> ReadWord()
    {
        var currentIndex = 0;
        TakeWhile(char.IsWhiteSpace);
        while (true)
        {
            if (!TryPeekChar(currentIndex, out var nextChar) || !char.IsLetter(nextChar))
            {
                break;
            }

            currentIndex++;
        }

        return SetNewSourceIndex(currentIndex);
    }

    private ReadOnlySpan<char> SetNewSourceIndex(int nextIndex)
    {
        var result = Source[..nextIndex];
        Source = Source[nextIndex..];
        return result;
    }
    
    public ReadOnlySpan<char> ReadTo(string to)
    {
        if (to.Length == 0)
        {
            return Source;
        }
        
        var index = 0;
        var toIndex = 0;
        
        while (true)
        {
            if (TryPeekChar(index, out var nextChar))
            {
                index++;
                if (nextChar != to[toIndex])
                {
                    toIndex = 0;
                }
                else
                {
                    toIndex++;
                    if (to.Length != toIndex)
                    {
                        continue;
                    }

                    return SetNewSourceIndex(index);
                }
            }
            else
            {
                return Source;
            }
        }
    }
    
    public ReadOnlySpan<char> TakeWhile(Func<char, bool> predicate)
    {
        var index = 0;
        while (true)
        {
            if (TryPeekChar(index, out var nextChar) && predicate(nextChar))
            {
                index++;
            }
            else
            {
                return SetNewSourceIndex(index);
            }
        }
    }

    public char Read()
    {
        if (!TryRead(out var nextChar))
        {
            throw new InvalidOperationException();
        }
        
        return nextChar;
    }
    
    public bool TryRead(out char nextChar)
    {
        if (!TryPeekChar(0, out nextChar))
        {
            return false;
        }
        
        Source = Source[1..];
        return true;
    }
    
    public bool StartsWith(ReadOnlySpan<char> str)
    {
        if (str.Length == 0)
        {
            return true;
        }
        
        var index = 0;
        while (true)
        {
            if (!TryPeekChar(index, out var nextChar))
            {
                return false;
            }

            if (nextChar != str[index])
            {
                return false;
            }
            
            index++;
            if (str.Length == index)
            {
                return true;
            }
        }
    }

    public bool TryPop()
    {
        if (!TryPeekChar(0, out _))
        {
            return false;
        }
        
        Source = Source[1..];
        return true;

    }

    public bool TryPop(char next)
    {
        return TryPop(new ReadOnlySpan<char>([next]));
    }
    
    public bool TryPop(ReadOnlySpan<char> str)
    {
        if (!StartsWith(str))
        {
            return false;
        }
        
        Source = Source[str.Length..];
        return true;
    }
    
    public void Pop()
    {
        if (!TryPeekChar(0, out _))
        {
            throw new InvalidOperationException();
        }
        
        Source = Source[1..];
    }

    public bool TryPeekChar(char tryPeekChar)
    {
        return TryPeekChar(0, out var c) && tryPeekChar == c;
    }
    
    public bool TryPeekChar(out char c)
    {
        return TryPeekChar(0 , out c);
    }
    
    public bool TryPeekChar(int index, out char c)
    {
        if (Source.Length <= index)
        {
            c = '\0';
            return false;
        }
        
        c = Source[index];
        return true;
    }
}

public static class StringExtensions
{
    public static StringBuilder ToKebabCase(this string source)
    {
        var reader = new SpanReader(source);
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
    
    public static StringBuilder ToCamelCase(this string source)
    {
        var reader = new SpanReader(source);
        var sb = new StringBuilder();

        var firstCharProcessed = false;

        while (reader.TryRead(out var nextChar))
        {
            sb.Append(firstCharProcessed ? nextChar : char.ToLower(nextChar));
            firstCharProcessed = true;
        }

        return sb;
    }
}
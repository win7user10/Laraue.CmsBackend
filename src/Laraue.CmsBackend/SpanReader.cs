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

                    var result = Source[..index];
                    Source = Source[index..];
                    return result;
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
                var result = Source[..index];
                Source = Source[index..];
                return result;
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
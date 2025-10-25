using Laraue.Interpreter.Scanning;

namespace Laraue.CmsBackend.MarkdownTransformation;

public class MdTokenScanner(string input)
    : TokenScanner<MdTokenType>(input)
{
    protected override bool TryProcessNextChar(char nextChar)
    {
        switch (nextChar)
        {
            case '\r':
                if (PopNextCharIf(c => c == '\n'))
                {
                    AddToken(MdTokenType.NewLine);
                    ToNextLine();
                }
                return true;
            case ' ':
                if (!PopNextCharIf(c => c == ' '))
                {
                    AddToken(MdTokenType.Whitespace);   // ' '
                    return true;
                }
                if (!PopNextCharIf(c => c == ' '))
                {
                    AddToken(MdTokenType.LineBreak); // '  '
                    return true;
                }
                if (!PopNextCharIf(c => c == ' '))
                {
                    AddToken(MdTokenType.Whitespace); // '   '
                    AddToken(MdTokenType.LineBreak);
                    return true;
                }
                
                AddToken(MdTokenType.Ident); // '    '
                return true;
            case '*':
                AddToken(PopNextCharIf(c => c == '*') ? MdTokenType.DoubleAsterisk : MdTokenType.Asterisk);
                return true;
            case '#':
                AddToken(MdTokenType.NumberSign);
                return true;
            case '.':
                AddToken(MdTokenType.Dot);
                return true;
            case '[':
                AddToken(MdTokenType.StartArray);
                return true;
            case ':':
                AddToken(MdTokenType.Delimiter);
                return true;
            case ']':
                AddToken(MdTokenType.EndArray);
                return true;
            case ',':
                AddToken(MdTokenType.Comma);
                return true;
            case '`':
                AddToken(MdTokenType.Backtick);
                return true;
            case '-':
                AddToken(MdTokenType.MinusSign);
                return true;
            case '|':
                AddToken(MdTokenType.Pipe);
                return true;
            case '_':
                AddToken(PopNextCharIf(c => c == '_') ? MdTokenType.DoubleUnderscore : MdTokenType.Underscore);
                return true;
            default:
                AddWordOrNumber();
                return true;
        }
    }
    
    private void AddWordOrNumber()
    {
        var anyDigit = Check(-1, IsDigit);
        while (PopNextCharIf(IsDigit));

        if (anyDigit) // If the token only with digits - it is digit
        {
            if (!Check(0, IsAlpha))
            {
                AddToken(
                    MdTokenType.Number,
                    int.Parse(GetCurrentScanValue()));
                return;
            }
        }
        
        while (PopNextCharIf(ch => IsDigit(ch) || IsAlpha(ch)))
        {
        }
        
        var text = GetCurrentScanValue();
        AddToken(MdTokenType.Word, text);
    }
}

public enum MdTokenType
{
    /// <summary>
    /// '*'
    /// </summary>
    Asterisk,
    
    /// <summary>
    /// '**'
    /// </summary>
    DoubleAsterisk,
    
    /// <summary>
    /// '`'
    /// </summary>
    Backtick,
    
    /// <summary>
    /// '#'
    /// </summary>
    NumberSign,
    
    /// <summary>
    /// '['
    /// </summary>
    StartArray,
    
    /// <summary>
    /// ']'
    /// </summary>
    EndArray,
    
    /// <summary>
    /// '-'
    /// </summary>
    MinusSign,
    
    /// <summary>
    /// ' '
    /// </summary>
    Whitespace,
    
    /// <summary>
    /// '  '
    /// </summary>
    LineBreak,
    
    /// <summary>
    /// '    '
    /// </summary>
    Ident,
    
    /// <summary>
    /// '\r\n'
    /// </summary>
    NewLine,
    
    /// <summary>
    /// '(\w\d)+'
    /// </summary>
    Word,
    
    /// <summary>
    /// ':'
    /// </summary>
    Delimiter,
    
    /// <summary>
    /// ','
    /// </summary>
    Comma,
    
    /// <summary>
    /// '.'
    /// </summary>
    Dot,
    
    /// <summary>
    /// '_'
    /// </summary>
    Underscore,
    
    /// <summary>
    /// '__'
    /// </summary>
    DoubleUnderscore,
    
    /// <summary>
    /// (\d+).(\d+)
    /// </summary>
    Number,
    
    /// <summary>
    /// '|'
    /// </summary>
    Pipe,
}
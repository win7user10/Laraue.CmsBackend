namespace Laraue.CmsBackend;

public class FilePath
{
    public readonly string[] Segments;
    private readonly string _stringValue;
    
    public FilePath(string[] segments)
    {
        Segments = segments;
        _stringValue = string.Join(Path.DirectorySeparatorChar, segments);
    }

    public static implicit operator string(FilePath filePath)
    {
        return string.Join(Path.DirectorySeparatorChar, filePath.Segments);
    }

    public override bool Equals(object? obj)
    {
        return obj switch
        {
            null => false,
            string stringObject => string.Equals(_stringValue, stringObject),
            FilePath filePath => Segments.SequenceEqual(filePath.Segments),
            _ => false
        };
    }

    public override int GetHashCode()
    {
        return _stringValue.GetHashCode();
    }

    public override string ToString()
    {
        return _stringValue;
    }
}
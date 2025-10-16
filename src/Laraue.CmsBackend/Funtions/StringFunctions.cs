namespace Laraue.CmsBackend.Funtions;

public class StringFunctions
{
    [CmsBackendFunction("substring")]
    public static string Substring(string str, int start, int length)
    {
        return str.Substring(start, length);
    }
    
    [CmsBackendFunction("length")]
    public static int Length(string str)
    {
        return str.Length;
    }
}
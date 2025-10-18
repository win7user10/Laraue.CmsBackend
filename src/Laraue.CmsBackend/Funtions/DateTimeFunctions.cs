namespace Laraue.CmsBackend.Funtions;

public class DateTimeFunctions
{
    [CmsBackendFunction("format")]
    public static string Format(DateTime date, string format)
    {
        return date.ToString(format);
    }
}
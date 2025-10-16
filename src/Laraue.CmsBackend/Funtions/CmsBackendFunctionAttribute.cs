namespace Laraue.CmsBackend.Funtions;

[AttributeUsage(AttributeTargets.Method)]
public class CmsBackendFunctionAttribute : Attribute
{
    public string Name { get; }

    public CmsBackendFunctionAttribute(string name)
    {
        Name = name;
    }
}
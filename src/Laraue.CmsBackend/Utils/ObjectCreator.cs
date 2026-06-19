using System.Reflection;

namespace Laraue.CmsBackend.Utils;

public static class ObjectCreator
{
    /// <summary>
    /// Map the passed dictionary to the provided type. 
    /// </summary>
    /// <param name="dict"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static object Initialize(Dictionary<string, object> dict, Type type)
    {
        var obj = Activator.CreateInstance(type);

        foreach (var kv in dict)
        {
            var prop = type.GetProperty(kv.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
                continue;

            var value = kv.Value;
            if (value is Dictionary<string, object> objects)
                value = Initialize(objects, prop.PropertyType);

            prop.SetValue(obj, value, null);
        }
        
        return obj!;
    }
    
    public static T Initialize<T>(Dictionary<string, object> dict)
    {
        return (T)ObjectCreator.Initialize(dict, typeof(T));
    }
}
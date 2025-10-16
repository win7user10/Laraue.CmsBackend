using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Laraue.CmsBackend.Contracts;

namespace Laraue.CmsBackend.Funtions;

public class CmsBackendFunctionsRegistry
{
    private readonly Dictionary<(string, ContentTypePropertyType), List<Method>> _methods = new ();
    
    public void AddFunctionsSupport(Type functionsClass)
    {
        var importMethods = functionsClass
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.GetCustomAttribute<CmsBackendFunctionAttribute>() != null)
            .ToArray();

        var exceptions = new List<Exception>();
        foreach (var method in importMethods)
        {
            try
            {
                AddFunctionSupport(method);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException(exceptions);
        }
    }

    // Check, if the passed property is actually function then should return new property name + delegate that 
    // consumes the object and return a new object to make the further comparison
    public bool TryParsePropertyAsFunction(string propertyValue, [NotNullWhen(true)] out FunctionParameters? function)
    {
        var reader = new SpanReader(propertyValue);
        
        var identifier = reader.ReadWord();
        if (!reader.TryPop('('))
        {
            function = null;
            return false;
        }

        var otherParameters = new List<string>();
        string? objectParameterName = null;
        
        do
        {
            var nextWord = reader.ReadIdentifier();
            if (objectParameterName is null)
            {
                objectParameterName = nextWord.ToString();
            }
            else
            {
                otherParameters.Add(nextWord.ToString());
            }
            
        } while (reader.TryPop(','));

        if (!reader.TryPop(')'))
        {
            throw new InvalidMethodException("Wrong syntax");
        }

        function = new FunctionParameters
        {
            FunctionName = identifier.ToString(),
            OtherParameters = otherParameters.ToArray(),
            ParameterName = objectParameterName ?? throw new InvalidMethodException("Parameter argument should be passed"),
        };
        return true;
    }

    public bool TryGetDelegate(
        FunctionParameters function,
        object realValue,
        [NotNullWhen(true)] out MethodInfo? executeFunction)
    {
        var valueType = realValue.GetType().GetCmsPropertyType();

        if (!TryFindFunction(function.FunctionName, valueType, function.OtherParameters.Length, out var func))
        {
            executeFunction = null;
            return false;
        }

        executeFunction = func.MethodInfo;
        return true;
    }

    public bool TryExecuteDelegate(
        FunctionParameters function,
        object realValue,
        [NotNullWhen(true)] out object? newValue)
    {
        if (!TryGetDelegate(function, realValue, out var func))
        {
            newValue = null;
            return false;
        }

        var parametersList = new List<object>
        {
            realValue
        };
        
        foreach (var otherParameter in function.OtherParameters)
        {
            parametersList.Add(ParseParameter(otherParameter));
        }

        newValue = func.Invoke(null, parametersList.ToArray())!;
        return true;
    }

    private object ParseParameter(string parameter)
    {
        if (parameter.StartsWith("\""))
        {
            return parameter[1..^1];
        }
        
        if (int.TryParse(parameter, out var number))
        {
            return number;
        }

        if (double.TryParse(parameter, out var doubleNumber))
        {
            return doubleNumber;
        }

        throw new InvalidMethodException("Unknown parameter type");
    }
    
    private bool TryFindFunction(
        string functionName,
        ContentTypePropertyType propertyType,
        int parameterCount,
        [NotNullWhen(true)] out Method? function)
    {
        function = null;
        if (!_methods.TryGetValue((functionName, propertyType), out var functions))
        {
            return false;
        }

        var suitableMethod = functions.FirstOrDefault(f => f.ArgumentTypes.Length == parameterCount);
        if (suitableMethod is null)
        {
            return false;
        }

        function = suitableMethod;
        return true;
    }

    public class FunctionParameters
    {
        public required string FunctionName { get; set; }
        public required string ParameterName { get; set; }
        public required string[] OtherParameters { get; set; }
    }

    private void AddFunctionSupport(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            throw new InvalidMethodException(
                $"Method {method.Name} should contain at least one parameter that is actually invoking member");
        }

        if (method.ReturnType == typeof(void))
        {
            throw new InvalidMethodException(
                $"Method {method.Name} does not returns anything");
        }
        
        var objectType = parameters[0].ParameterType.GetCmsPropertyType();
        var returnType = method.ReturnType.GetCmsPropertyType();
        var parameterTypes = parameters
            .Skip(1)
            .Select(p => p.ParameterType.GetCmsPropertyType())
            .ToArray();

        var attribute = method.GetCustomAttribute<CmsBackendFunctionAttribute>();
        var methodName = attribute!.Name;

        if (!_methods.ContainsKey((methodName, objectType)))
        {
            _methods[(methodName, objectType)] = [];
        }
        
        _methods[(methodName, objectType)].Add(new Method
        {
            ReturnType = returnType,
            ArgumentTypes = parameterTypes,
            ObjectType = objectType,
            MethodInfo = method,
        });
    }

    private class Method
    {
        public required ContentTypePropertyType ObjectType { get; set; }
        public required ContentTypePropertyType ReturnType { get; set; }
        public required ContentTypePropertyType[] ArgumentTypes { get; set; }
        public required MethodInfo MethodInfo { get; set; }
    }
}
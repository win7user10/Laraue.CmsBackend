using Laraue.CmsBackend.Funtions;

namespace Laraue.CmsBackend;

public static class PropertyParser
{
    public static PropertyData ParsePropertyParameters(string propertyValue)
    {
        var reader = new SpanReader(propertyValue);
        
        var identifier = reader.ReadWord();
        if (!reader.TryPop('('))
        {
            return new PropertyData
            {
                PropertyName = identifier.ToString(),
                Alias = GetAlias(ref reader),
            };
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

        return new PropertyData
        {
            Alias = GetAlias(ref reader),
            FunctionParameters = new FunctionParameters()
            {
                FunctionName = identifier.ToString(),
                OtherParameters = otherParameters.ToArray(),
                CalleeName = objectParameterName ?? throw new InvalidMethodException("Parameter argument should be passed"),
            }
        };
    }

    private static string? GetAlias(ref SpanReader reader)
    {
        if (reader.IsEndOfFileReached)
        {
            return null;
        }
        
        var nextWord = reader.ReadWord();
        if (nextWord.ToString() != "as")
        {
            throw new InvalidMethodException("End of property or alias excepted");
        }
        
        return reader.ReadWord().ToString();
    }
}

public class FunctionParameters
{
    public required string FunctionName { get; set; }
    public required string CalleeName { get; set; }
    public required string[] OtherParameters { get; set; }
}

public class PropertyData
{
    public FunctionParameters? FunctionParameters { get; set; }
    public string? Alias { get; set; }
    public string? PropertyName { get; set; }
}
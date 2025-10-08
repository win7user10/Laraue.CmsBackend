namespace Laraue.CmsBackend.Extensions;

public static class CmsBackendBuilderExtensions
{
    public static ICmsBackendBuilder AddContentFolder(
        this ICmsBackendBuilder builder,
        string path)
    {
        var filesIterator = Directory.EnumerateFiles(path, "*.md", SearchOption.AllDirectories);
        
        var errors = new List<AddContentFolderFileException>();
        
        foreach (var filePath in filesIterator)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var directoryName = Path.GetDirectoryName(filePath);
            
            var fileContent = File.ReadAllText(filePath);
            var modifiedAt = File.GetLastWriteTimeUtc(filePath);
            
            try
            {
                builder.AddContent(fileContent, directoryName!, fileName, modifiedAt);
            }
            catch (MarkdownParserException e)
            {
                errors.Add(new AddContentFolderFileException($"Error while adding file '{filePath}'", e));
            }
        }

        if (errors.Count != 0)
        {
            throw new AggregateException($"Folder '{path}' content adding finished with errors", errors);
        }
        
        return builder;
    }
}

public class AddContentFolderFileException : Exception
{
    public AddContentFolderFileException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
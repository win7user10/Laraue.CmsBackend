using Laraue.CmsBackend.Contracts;

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
            var directorySegments = new FilePath(directoryName!.Split(Path.DirectorySeparatorChar));
            
            var fileContent = File.ReadAllText(filePath);
            
            try
            {
                builder.AddContent(
                    new ContentProperties(
                        fileContent,
                        directorySegments,
                        fileName));
            }
            catch (MarkdownParserException e)
            {
                errors.Add(new AddContentFolderFileException($"'{filePath}'", e));
            }
        }

        if (errors.Count != 0)
        {
            throw new AggregateException($"Folder '{path}' has content with errors", errors);
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
using System.Diagnostics.CodeAnalysis;
using Laraue.CmsBackend.Contracts;

namespace Laraue.CmsBackend;

public class ProcessedMdFileRegistry
{
    private readonly Node _hierarchy = new();
    
    private const string IndexFileName = "index";

    public bool TryAdd(ProcessedMdFile mdFile)
    {
        var filePath = (FilePath)mdFile["path"];
        var fileName = (string)mdFile["fileName"];
        
        var allSegments = fileName == IndexFileName
            ? filePath.Segments
            : filePath.Segments.Append(fileName).ToArray();

        var node = _hierarchy;
        foreach (var pathSegment in allSegments)
        {
            if (!node.HasNode(pathSegment))
            {
                node.Children.Add(new Node
                {
                    FileName = pathSegment,
                });
            }
            
            node = node.GetNode(pathSegment);
        }

        if (node.NodeMdFile is not null)
        {
            return false;
        }
        
        node.NodeMdFile = mdFile;
        return true;
    }

    public bool TryGet(string[] path, [NotNullWhen(true)] out ProcessedMdFile? mdFile)
    {
        if (TryGetNodeByPath(path, out var node) && node.NodeMdFile is not null)
        {
            mdFile = node.NodeMdFile;
            return true;
        }

        mdFile = null;
        return false;
    }

    public IEnumerable<ProcessedMdFile> GetEntities(string[]? path)
    {
        return GetSubNodesByPath(path ?? [], int.MaxValue)
            .Where(node => node.NodeMdFile != null)
            .Select(node => node.NodeMdFile!);
    }
    
    private bool TryGetNodeByPath(string[] fromPath, [NotNullWhen(true)] out Node? result)
    {
        var node = _hierarchy;
        
        // descend to the requested path
        foreach (var pathSegment in fromPath)
        {
            if (!node.TryGetNode(pathSegment, out node))
            {
                result = null;
                return false;
            }
        }
        
        result = node;
        return true;
    }
    
    private IEnumerable<Node> GetSubNodesByPath(string[] fromPath, int depth)
    {
        var node = _hierarchy;
        
        foreach (var pathSegment in fromPath)
        {
            if (!node.TryGetNode(pathSegment, out node))
            {
                return [];
            }
        }

        var result = new List<Node>();
        var nextNodes = node.Children;
        
        while (depth > 0)
        {
            result.AddRange(nextNodes);
            nextNodes = nextNodes.SelectMany(x => x.Children).ToList();
            if (nextNodes.Count == 0)
            {
                break;
            }
            depth--;
        }

        return result;
    }
    
    public IEnumerable<SubSectionItem> GetSubSections(string[] fromPath, int depth)
    {
        var node = _hierarchy;
        
        // descend to the requested path
        foreach (var pathSegment in fromPath)
        {
            if (!node.TryGetNode(pathSegment, out node))
            {
                return [];
            }
        }

        var result = new List<SubSectionItem>();
        AppendSubSections(result, node, depth, fromPath, []);
        
        return result;
    }

    private void AppendSubSections(
        List<SubSectionItem> destination,
        Node attachedNode,
        int depth,
        string[] requestedPath,
        string[] currentRelativePath)
    {
        if (depth < 1)
        {
            return;
        }

        if (attachedNode.Children.Count == 0)
        {
            return;
        }
        
        foreach (var child in attachedNode.Children)
        {
            var children = new List<SubSectionItem>();
            var nextPath = currentRelativePath.Append(child.FileName!).ToArray();
            AppendSubSections(children, child, depth - 1, requestedPath, nextPath);

            object? title = null;
            child.NodeMdFile?.TryGetValue("title", out title);
            
            destination.Add(new SubSectionItem
            {
                FileName = child.FileName!,
                Children = children.ToArray(),
                FullPath = requestedPath.Union(nextPath).ToArray(),
                HasContent = child.NodeMdFile is not null && ((string)child.NodeMdFile["content"]).Length > 0,
                Title = title as string,
                RelativePath = nextPath
            });
        } 
    }

    public class SubSectionItem
    {
        public required string FileName { get; set; }
        public required string[] RelativePath { get; set; }
        public required string[] FullPath { get; set; }
        public required SubSectionItem[] Children { get; set; }
        public required bool HasContent { get; set; }
        public required string? Title { get; set; }
    }

    private record Node
    {
        public string? FileName { get; set; }
        public ProcessedMdFile? NodeMdFile { get; set; }
        public List<Node> Children { get; set; } = new();

        public bool HasNode(string name)
        {
            return Children.Any(x => x.FileName == name);
        }
        
        public Node GetNode(string name)
        {
            if (!TryGetNode(name, out var node))
            {
                throw new InvalidOperationException();
            }
            
            return node;
        }
        
        public bool TryGetNode(string name, [NotNullWhen(true)] out Node? node)
        {
            node = Children.FirstOrDefault(x => x.FileName == name);
            return node != null;
        }
    }
}
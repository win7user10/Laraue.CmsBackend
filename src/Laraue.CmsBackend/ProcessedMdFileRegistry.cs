using System.Diagnostics.CodeAnalysis;
using Laraue.CmsBackend.Contracts;

namespace Laraue.CmsBackend;

public class ProcessedMdFileRegistry
{
    private readonly Node _hierarchy = new();

    public bool TryAdd(ProcessedMdFile mdFile)
    {
        var filePath = (string[])mdFile["path"];

        var node = _hierarchy;
        foreach (var pathSegment in filePath)
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

        return node.NodeFiles.TryAdd((string)mdFile["languageCode"], mdFile);
    }

    public bool TryGet(
        string languageCode,
        string[] path,
        [NotNullWhen(true)] out ProcessedMdFile? mdFile)
    {
        if (TryGetNodeByPath(path, out var node) && node.NodeFiles.TryGetValue(languageCode, out mdFile))
        {
            return true;
        }

        mdFile = null;
        return false;
    }

    public IEnumerable<ProcessedMdFile> GetEntities(
        string languageCode,
        string[]? path)
    {
        return GetSubNodesByPath(path ?? [], int.MaxValue)
            .Where(node => node.NodeFiles.ContainsKey(languageCode))
            .Select(node => node.NodeFiles[languageCode]);
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
    
    public IEnumerable<SubSectionItem> GetSubSections(
        string languageCode,
        string[] fromPath,
        int depth)
    {
        var node = _hierarchy;
        
        // descend to the requested path
        foreach (var pathSegment in fromPath)
            if (!node.TryGetNode(pathSegment, out node))
                return [];

        var result = new List<SubSectionItem>();
        AppendSubSections(languageCode, result, node, depth, fromPath, []);
        
        return result;
    }

    private void AppendSubSections(
        string languageCode,
        List<SubSectionItem> destination,
        Node attachedNode,
        int depth,
        string[] requestedPath,
        string[] currentRelativePath)
    {
        if (depth < 1)
            return;

        if (attachedNode.Children.Count == 0)
            return;
        
        foreach (var child in attachedNode.Children)
        {
            var children = new List<SubSectionItem>();
            var nextPath = currentRelativePath.Append(child.FileName!).ToArray();
            AppendSubSections(languageCode, children, child, depth - 1, requestedPath, nextPath);

            object? title = null;
            
            child.NodeFiles.TryGetValue(languageCode, out var contentNode);
            contentNode?.TryGetValue("title", out title);
            
            destination.Add(new SubSectionItem
            {
                FileName = child.FileName!,
                Children = children.ToArray(),
                FullPath = requestedPath.Union(nextPath).ToArray(),
                HasContent = contentNode is not null && ((string)contentNode["content"]).Length > 0,
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
        public NodeFiles NodeFiles { get; set; } = new();
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

    private class NodeFiles : Dictionary<string, ProcessedMdFile>
    {
    }
}
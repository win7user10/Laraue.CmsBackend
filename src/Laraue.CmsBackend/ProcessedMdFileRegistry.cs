using System.Diagnostics.CodeAnalysis;
using Laraue.CmsBackend.Contracts;

namespace Laraue.CmsBackend;

public class ProcessedMdFileRegistry
{
    private readonly Dictionary<MdFileKey, ProcessedMdFile> _processedMdFiles =  new ();
    private readonly Node _hierarchy = new();

    public bool TryAdd(ProcessedMdFile mdFile)
    {
        var key = new MdFileKey
        {
            Id = (string)mdFile["id"],
            ContentType = (string)mdFile["contentType"]
        };
        
        if (!_processedMdFiles.TryAdd(key, mdFile))
        {
            return false;
        }

        var pathSegments = (string[])mdFile["path"];

        var node = _hierarchy;
        foreach (var pathSegment in pathSegments)
        {
            if (!node.HasNode(pathSegment))
            {
                node.Children.Add(new Node
                {
                    Segment = pathSegment,
                });
            }
            
            node = node.GetNode(pathSegment);
        }
        
        node.NodeMdFile = mdFile;
        return true;
    }

    public bool TryGet(MdFileKey key, [NotNullWhen(true)] out ProcessedMdFile? mdFile)
    {
        return _processedMdFiles.TryGetValue(key, out mdFile);
    }

    public IEnumerable<ProcessedMdFile> GetEntities()
    {
        return _processedMdFiles.Values;
    }
    
    public IEnumerable<SubSectionItem> GetSubSections(string? fromPath, int depth)
    {
        var node = _hierarchy;
        var pathSections = fromPath?.Split('/') ?? [];
        
        // descend to the requested path
        foreach (var pathSegment in pathSections)
        {
            if (!node.TryGetNode(pathSegment, out node))
            {
                return [];
            }
        }

        var result = new List<SubSectionItem>();
        AppendSubSections(result, node, depth, pathSections);
        
        return result;
    }

    private void AppendSubSections(
        List<SubSectionItem> destination,
        Node attachedNode,
        int depth,
        string[] currentPath)
    {
        if (depth < 1)
        {
            return;
        }

        if (attachedNode.Children.Count == 0)
        {
            return;
        }
        
        var children = new List<SubSectionItem>();
        
        foreach (var child in attachedNode.Children)
        {
            var nextPath = currentPath.Append(child.Segment!).ToArray();
            var nextPathString = string.Join("/", nextPath);
            AppendSubSections(children, child, depth - 1, nextPath);
            destination.Add(new SubSectionItem
            {
                RelativePath = child.Segment!,
                Children = children.ToArray(),
                FullPath = nextPathString,
                HasContent = child.NodeMdFile is not null
            });
        } 
    }

    public class SubSectionItem
    {
        public required string RelativePath { get; set; }
        public required string FullPath { get; set; }
        public required SubSectionItem[] Children { get; set; }
        public required bool HasContent { get; set; }
    }

    private record Node
    {
        public string? Segment { get; set; }
        public ProcessedMdFile? NodeMdFile { get; set; }
        public List<Node> Children { get; set; } = new();

        public bool HasNode(string name)
        {
            return Children.Any(x => x.Segment == name);
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
            node = Children.FirstOrDefault(x => x.Segment == name);
            return node != null;
        }
    }
}
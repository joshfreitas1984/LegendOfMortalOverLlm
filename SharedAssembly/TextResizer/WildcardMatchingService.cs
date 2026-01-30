//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace SharedAssembly.TextResizer;

//public class WildcardMatchingService
//{
//    private readonly TrieNode _root = new TrieNode();

//    public WildcardMatchingService(List<TextResizerContract> contracts)
//    {
//        // Build the trie structure
//        foreach (var contract in contracts)
//        {
//            AddPatternToTrie(contract.Path, contract);
//        }
//    }

//    public TextResizerContract? FindMatch(string path)
//    {
//        var segments = path.Split('\\', StringSplitOptions.RemoveEmptyEntries);
//        return MatchPath(_root, segments, 0);
//    }

//    private void AddPatternToTrie(string pattern, TextResizerContract contract)
//    {
//        var segments = pattern.Split('\\', StringSplitOptions.RemoveEmptyEntries);
//        AddPatternToNode(_root, segments, 0, contract);
//    }

//    private void AddPatternToNode(TrieNode node, string[] segments, int index, TextResizerContract contract)
//    {
//        if (index == segments.Length)
//        {
//            node.Contracts.Add(contract);
//            return;
//        }

//        var segment = segments[index];

//        if (!node.Children.TryGetValue(segment, out var childNode))
//        {
//            childNode = new TrieNode();
//            node.Children[segment] = childNode;
//        }

//        AddPatternToNode(childNode, segments, index + 1, contract);
//    }

//    private TextResizerContract? MatchPath(TrieNode node, string[] segments, int index)
//    {
//        if (index == segments.Length)
//        {
//            return node.Contracts.FirstOrDefault();
//        }

//        var segment = segments[index];
//        TextResizerContract? result = null;

//        // Try exact match first
//        if (node.Children.TryGetValue(segment, out var exactMatchNode))
//        {
//            result = MatchPath(exactMatchNode, segments, index + 1);
//            if (result != null)
//                return result;
//        }

//        // Try wildcard match for zero or more segments
//        if (node.Children.TryGetValue("*", out var wildcardNode))
//        {
//            for (int i = index; i <= segments.Length; i++)
//            {
//                result = MatchPath(wildcardNode, segments, i);
//                if (result != null)
//                    return result;
//            }
//        }

//        return null;
//    }

//    private class TrieNode
//    {
//        public Dictionary<string, TrieNode> Children { get; } = new Dictionary<string, TrieNode>();
//        public List<TextResizerContract> Contracts { get; } = new List<TextResizerContract>();
//    }
//}

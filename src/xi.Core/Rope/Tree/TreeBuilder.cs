using System;
using System.Collections.Generic;
using System.Text;

namespace Xi.Core.Rope.Tree;

/// <summary>
/// Incremental builder that mirrors xi-editor's stack-based TreeBuilder to maintain B-tree invariants.
/// </summary>
public sealed class TreeBuilder
{
    private readonly List<List<Node>> _stack = new();
    private const int LeafPreviewLimit = 32;
    private static readonly UTF8Encoding Utf8NoBomEncoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    /// <summary>
    /// TODO(TS-B2): Replace the no-op tracer with a Stage&nbsp;D slice trace once Rust/C# manifests align.
    /// </summary>
    internal ITreeBuilderTracer Tracer { get; set; } = NoOpTreeBuilderTracer.Instance;

    public void PushString(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        foreach (var segment in SplitIntoLeaves(text))
        {
            PushLeafSegment(segment);
        }
    }

    public void PushSpan(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
        {
            return;
        }

        PushString(new string(span));
    }

    public void PushNode(Node node)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (node.IsEmpty)
        {
            return;
        }

        PushNodeInternal(node);
        TraceNodeEvent(TreeBuilderEventKind.PushNode, node);
    }

    public Node Build()
    {
        if (_stack.Count == 0)
        {
            TraceNodeEvent(TreeBuilderEventKind.BuildCompleted, Node.Empty, stackDepthOverride: 0);
            return Node.Empty;
        }

        var result = PopStackNode();
        while (_stack.Count > 0)
        {
            result = Node.Concat(PopStackNode(), result);
        }

        TraceNodeEvent(TreeBuilderEventKind.BuildCompleted, result, stackDepthOverride: 0);
        return result;
    }

    public void Reset()
    {
        _stack.Clear();
        TraceNodeEvent(TreeBuilderEventKind.Reset, Node.Empty, stackDepthOverride: 0);
    }

    private void PushLeafSegment(string segment)
    {
        if (string.IsNullOrEmpty(segment))
        {
            return;
        }

        var leaf = Node.FromLeaf(segment);
        if (leaf.IsEmpty)
        {
            return;
        }

        PushNodeInternal(leaf);
        TraceNodeEvent(TreeBuilderEventKind.PushLeaf, leaf, segment);
    }

    private void PushNodeInternal(Node node)
    {
        var current = node;
        while (true)
        {
            var frame = _stack.Count > 0 ? _stack[^1] : null;
            if (frame == null)
            {
                _stack.Add(new List<Node> { current });
                break;
            }

            var comparison = frame[0].Height.CompareTo(current.Height);
            if (comparison < 0)
            {
                var popped = PopStackNode();
                current = Node.Concat(popped, current);
                continue;
            }

            if (comparison == 0)
            {
                var canExtend = frame[^1].IsOkChild() && current.IsOkChild();
                if (canExtend)
                {
                    frame.Add(current);
                    if (frame.Count < Node.MaxChildCount)
                    {
                        break;
                    }

                    current = PopStackNode();
                    continue;
                }

                if (current.Height == 0)
                {
                    MergeLeafIntoFrame(frame, current);
                    break;
                }

                MergeInternalIntoFrame(frame, current);
                if (frame.Count < Node.MaxChildCount)
                {
                    break;
                }

                current = PopStackNode();
                continue;
            }

            _stack.Add(new List<Node> { current });
            break;
        }
    }

    private Node PopStackNode()
    {
        if (_stack.Count == 0)
        {
            throw new InvalidOperationException("Cannot pop from an empty TreeBuilder stack.");
        }

        var lastIndex = _stack.Count - 1;
        var frame = _stack[lastIndex];
        _stack.RemoveAt(lastIndex);
        var node = frame.Count == 1 ? frame[0] : Node.FromNodes(frame);
        TraceNodeEvent(TreeBuilderEventKind.PopFrame, node);
        return node;
    }

    private void MergeLeafIntoFrame(List<Node> frame, Node incoming)
    {
        if (frame.Count == 0)
        {
            throw new InvalidOperationException("Cannot merge into an empty frame.");
        }

        if (!incoming.IsLeaf)
        {
            throw new InvalidOperationException("Leaf merge requires a leaf node.");
        }

        TraceNodeEvent(TreeBuilderEventKind.MergeLeaf, incoming);

        var baseIndex = frame.Count - 1;
        var baseLeaf = frame[baseIndex];
        if (!baseLeaf.IsLeaf)
        {
            throw new InvalidOperationException("Leaf merge requires a leaf base node.");
        }

        var baseText = baseLeaf.GetLeaf() ?? string.Empty;
        var incomingText = incoming.GetLeaf() ?? string.Empty;
        var mergedText = StringLeafOperations.Merge(baseText, incomingText);
        var segments = new List<Node>();

        foreach (var segment in StringLeafOperations.SplitByCapacity(mergedText))
        {
            if (segment.Length == 0)
            {
                continue;
            }

            segments.Add(Node.FromLeaf(segment));
        }

        if (segments.Count == 0)
        {
            frame.RemoveAt(baseIndex);
            return;
        }

        frame[baseIndex] = segments[0];
        for (var i = 1; i < segments.Count; i++)
        {
            frame.Add(segments[i]);
        }
    }

    private void MergeInternalIntoFrame(List<Node> frame, Node incoming)
    {
        if (frame.Count == 0)
        {
            throw new InvalidOperationException("Cannot merge internal nodes into an empty frame.");
        }

        if (incoming.IsLeaf)
        {
            throw new InvalidOperationException("Internal merge requires non-leaf nodes.");
        }

        TraceNodeEvent(TreeBuilderEventKind.MergeInternal, incoming);

        var lastIndex = frame.Count - 1;
        var last = frame[lastIndex];
        frame.RemoveAt(lastIndex);

        foreach (var node in CombineInternalChildren(last, incoming))
        {
            frame.Add(node);
        }
    }

    private static IReadOnlyList<Node> CombineInternalChildren(Node left, Node right)
    {
        var leftChildren = left.GetChildren();
        var rightChildren = right.GetChildren();
        if (leftChildren is null || rightChildren is null)
        {
            throw new InvalidOperationException("Internal merge requires child nodes.");
        }

        var total = leftChildren.Length + rightChildren.Length;
        var combined = new Node[total];
        Array.Copy(leftChildren, 0, combined, 0, leftChildren.Length);
        Array.Copy(rightChildren, 0, combined, leftChildren.Length, rightChildren.Length);

        if (total <= Node.MaxChildCount)
        {
            return new[] { Node.FromNodes(combined) };
        }

        var splitPoint = Math.Min(Node.MaxChildCount, total - Node.MinChildCount);
        if (splitPoint <= 0 || splitPoint >= total)
        {
            splitPoint = total / 2;
        }

        var leftSegment = new Node[splitPoint];
        Array.Copy(combined, 0, leftSegment, 0, splitPoint);

        var rightSegmentLength = total - splitPoint;
        var rightSegment = new Node[rightSegmentLength];
        Array.Copy(combined, splitPoint, rightSegment, 0, rightSegmentLength);

        return new[] { Node.FromNodes(leftSegment), Node.FromNodes(rightSegment) };
    }

    private static IEnumerable<string> SplitIntoLeaves(string text)
    {
        return LeafSplitter.Split(text);
    }

    private void TraceNodeEvent(TreeBuilderEventKind kind, Node node, string? leafTextOverride = null, int? stackDepthOverride = null)
    {
        if (!Tracer.IsEnabled)
        {
            return;
        }

        var stackDepth = stackDepthOverride ?? _stack.Count;
        var utf16Length = node.Info.Utf16Length;
        var byteLength = utf16Length;
        string? preview = null;

        if (node.IsLeaf)
        {
            var text = leafTextOverride ?? node.GetLeaf() ?? string.Empty;
            byteLength = text.Length == 0 ? 0 : Utf8NoBomEncoding.GetByteCount(text);
            preview = CreateLeafPreview(text);
        }

        var traceEvent = new TreeBuilderEvent(kind, stackDepth, node.Height, byteLength, utf16Length, preview);
        Tracer.Trace(traceEvent);
    }

    private static string? CreateLeafPreview(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (text.Length <= LeafPreviewLimit)
        {
            return text;
        }

        return text.AsSpan(0, LeafPreviewLimit).ToString();
    }
}

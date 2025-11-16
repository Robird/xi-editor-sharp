using System;
using System.Collections.Generic;
using Xi.Core.Rope;

namespace Xi.Core.Rope.Tree;

/// <summary>
/// Owning snapshot of a <see cref="NodeCursor"/> state that can be restored as long as the underlying nodes remain unchanged.
/// Mirrors xi-editor's <c>CursorDescriptor</c> semantics to support JSON parity fixtures and cross-language cursor caching.
/// </summary>
public sealed class CursorDescriptor
{
    private static readonly CursorDescriptorFrame[] s_emptyFrames = Array.Empty<CursorDescriptorFrame>();
    private readonly CursorDescriptorFrame[] _frames;

    private CursorDescriptor(bool isValid, int position, int offsetOfLeaf, int? leafLength, Node? leafNode, CursorDescriptorFrame[] frames)
    {
        IsValid = isValid;
        Position = position;
        OffsetOfLeaf = offsetOfLeaf;
        LeafLength = leafLength;
        LeafNode = leafNode;
        _frames = frames ?? throw new ArgumentNullException(nameof(frames));
    }

    /// <summary>Indicates whether the descriptor still tracks a valid leaf reference.</summary>
    public bool IsValid { get; }

    /// <summary>The absolute cursor position captured by this descriptor.</summary>
    public int Position { get; }

    /// <summary>The absolute offset of the leaf containing <see cref="Position"/>.</summary>
    public int OffsetOfLeaf { get; }

    /// <summary>The cached leaf length, when the descriptor is valid.</summary>
    public int? LeafLength { get; }

    internal Node? LeafNode { get; }

    /// <summary>The cached ancestor frames (root ⇒ leaf) limited by the cursor cache size.</summary>
    public IReadOnlyList<CursorDescriptorFrame> Frames => _frames;

    internal static CursorDescriptor CreateInvalid(int position)
    {
        return new CursorDescriptor(isValid: false, position, offsetOfLeaf: 0, leafLength: null, leafNode: null, s_emptyFrames);
    }

    internal static CursorDescriptor CreateValid(int position, int offsetOfLeaf, Node leafNode, IReadOnlyList<CursorDescriptorFrame> frames)
    {
        if (leafNode is null)
        {
            throw new ArgumentNullException(nameof(leafNode));
        }

        CursorDescriptorFrame[] snapshot;
        if (frames == null || frames.Count == 0)
        {
            snapshot = s_emptyFrames;
        }
        else
        {
            snapshot = new CursorDescriptorFrame[frames.Count];
            for (int i = 0; i < frames.Count; i++)
            {
                snapshot[i] = frames[i] ?? throw new ArgumentNullException(nameof(frames), "Descriptor frames cannot contain null entries.");
            }
        }

        return new CursorDescriptor(true, position, offsetOfLeaf, leafNode.Length, leafNode, snapshot);
    }

    /// <summary>
    /// Attempts to restore a <see cref="NodeCursor"/> bound to <paramref name="rope"/>.
    /// Returns <c>null</c> if the descriptor no longer matches the tree (e.g., the rope has been edited).
    /// </summary>
    public NodeCursor? TryRestore(Rope rope)
    {
        if (rope is null)
        {
            throw new ArgumentNullException(nameof(rope));
        }

        var clampedPosition = Math.Min(Position, rope.Length);
        var cursor = new NodeCursor(rope, clampedPosition);
        return cursor.TryApplyDescriptor(this) ? cursor : null;
    }
}

/// <summary>Represents a cached ancestor relationship captured inside a <see cref="CursorDescriptor"/>.</summary>
public sealed class CursorDescriptorFrame
{
    internal CursorDescriptorFrame(Node node, int childIndex, int childOffset)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        NodeHeight = node.Height;
        NodeLength = node.Length;
        ChildIndex = childIndex;
        ChildOffset = childOffset;
    }

    /// <summary>Height of the parent node when the descriptor was captured.</summary>
    public int NodeHeight { get; }

    /// <summary>Length of the parent node when the descriptor was captured.</summary>
    public int NodeLength { get; }

    /// <summary>Index of the traversed child within the cached parent.</summary>
    public int ChildIndex { get; }

    /// <summary>Accumulated byte/char offset contributed by preceding siblings.</summary>
    public int ChildOffset { get; }

    internal Node Node { get; }
}
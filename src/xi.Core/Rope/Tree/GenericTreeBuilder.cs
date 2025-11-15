using System;
using System.Collections.Generic;

namespace Xi.Core.Rope.Tree;

/// <summary>
/// M3 泛型 TreeBuilder：验证泛型节点接口兼容性
/// 与现有 TreeBuilder 并行存在，不触碰现有实现
/// M4 完成后可合并或替换字符串特化版本
/// </summary>
/// <typeparam name="TInfo">聚合元数据类型</typeparam>
/// <typeparam name="TLeaf">叶片负载类型</typeparam>
/// <typeparam name="TLeafOps">叶片操作 Helper</typeparam>
public sealed class GenericTreeBuilder<TInfo, TLeaf, TLeafOps>
    where TInfo : struct, ITreeNodeInfo<TInfo, TLeaf>
    where TLeafOps : ILeafOperations<TLeaf>
{
    private readonly List<Node<TInfo, TLeaf, TLeafOps>> _pending = new();

    /// <summary>
    /// 推入字符串（仅适用于 TLeaf = string）
    /// 内部通过 LeafSplitter 拆分后调用 TLeafOps 创建叶片
    /// </summary>
    public void PushString(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        // 拆分成符合叶片大小的片段
        foreach (var segment in LeafSplitter.Split(text))
        {
            // 通过 TLeafOps 将字符串转换为 TLeaf（对 string 类型为恒等转换）
            var leaf = ConvertStringToLeaf(segment);
            AppendNode(Node<TInfo, TLeaf, TLeafOps>.FromLeaf(leaf));
        }
    }

    /// <summary>
    /// 推入泛型节点
    /// </summary>
    public void PushNode(Node<TInfo, TLeaf, TLeafOps> node)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (node.IsEmpty)
        {
            return;
        }

        AppendNode(node);
    }

    /// <summary>
    /// 构建最终树
    /// </summary>
    public Node<TInfo, TLeaf, TLeafOps> Build()
    {
        if (_pending.Count == 0)
        {
            return Node<TInfo, TLeaf, TLeafOps>.Empty;
        }

        var result = _pending[0];
        for (var i = 1; i < _pending.Count; i++)
        {
            result = Concat(result, _pending[i]);
        }

        return result;
    }

    /// <summary>
    /// 重置构建器状态
    /// </summary>
    public void Reset() => _pending.Clear();

    private void AppendNode(Node<TInfo, TLeaf, TLeafOps> node)
    {
        var current = node;
        for (var index = _pending.Count - 1; index >= 0; index--)
        {
            var candidate = _pending[index];
            if (candidate.Height == current.Height)
            {
                current = Concat(candidate, current);
                _pending.RemoveAt(index);
            }
            else
            {
                break;
            }
        }

        _pending.Add(current);
    }

    /// <summary>
    /// 合并两个节点（完整实现，镜像 Node.Concat 逻辑）
    /// </summary>
    private static Node<TInfo, TLeaf, TLeafOps> Concat(
        Node<TInfo, TLeaf, TLeafOps> left,
        Node<TInfo, TLeaf, TLeafOps> right)
    {
        if (left.IsEmpty) return right;
        if (right.IsEmpty) return left;

        // 如果两个节点高度相同，直接创建父节点
        if (left.Height == right.Height)
        {
            return Node<TInfo, TLeaf, TLeafOps>.CreateInternal(new[] { left, right });
        }

        // 左节点较矮，插入到右节点最左侧
        if (left.Height < right.Height)
        {
            return ConcatLeftShorter(left, right);
        }

        // 右节点较矮，对称处理
        return ConcatRightShorter(left, right);
    }

    private static Node<TInfo, TLeaf, TLeafOps> ConcatLeftShorter(
        Node<TInfo, TLeaf, TLeafOps> left,
        Node<TInfo, TLeaf, TLeafOps> right)
    {
        var children = right.Children;
        var firstChild = children[0];
        var merged = Concat(left, firstChild);

        if (merged.Height == right.Height - 1)
        {
            // 合并后高度正好低一层，替换第一个子节点
            var newChildren = new Node<TInfo, TLeaf, TLeafOps>[children.Count];
            newChildren[0] = merged;
            for (int i = 1; i < children.Count; i++)
            {
                newChildren[i] = children[i];
            }
            return Node<TInfo, TLeaf, TLeafOps>.CreateInternal(newChildren);
        }

        if (merged.Height == right.Height)
        {
            // 合并后高度与右节点相同，需要展开并重新组合
            var mergedChildren = merged.Children;
            var combined = new Node<TInfo, TLeaf, TLeafOps>[mergedChildren.Count + children.Count - 1];
            for (int i = 0; i < mergedChildren.Count; i++)
            {
                combined[i] = mergedChildren[i];
            }
            for (int i = 1; i < children.Count; i++)
            {
                combined[mergedChildren.Count + i - 1] = children[i];
            }
            return Node<TInfo, TLeaf, TLeafOps>.CreateInternal(combined);
        }

        throw new InvalidOperationException(
            $"Unexpected height relationship during concatenation: " +
            $"left={left.Height}, right={right.Height}, merged={merged.Height}");
    }

    private static Node<TInfo, TLeaf, TLeafOps> ConcatRightShorter(
        Node<TInfo, TLeaf, TLeafOps> left,
        Node<TInfo, TLeaf, TLeafOps> right)
    {
        var children = left.Children;
        var lastChild = children[children.Count - 1];
        var merged = Concat(lastChild, right);

        if (merged.Height == left.Height - 1)
        {
            // 合并后高度正好低一层，替换最后一个子节点
            var newChildren = new Node<TInfo, TLeaf, TLeafOps>[children.Count];
            for (int i = 0; i < children.Count - 1; i++)
            {
                newChildren[i] = children[i];
            }
            newChildren[children.Count - 1] = merged;
            return Node<TInfo, TLeaf, TLeafOps>.CreateInternal(newChildren);
        }

        if (merged.Height == left.Height)
        {
            // 合并后高度与左节点相同，需要展开并重新组合
            var mergedChildren = merged.Children;
            var combined = new Node<TInfo, TLeaf, TLeafOps>[children.Count - 1 + mergedChildren.Count];
            for (int i = 0; i < children.Count - 1; i++)
            {
                combined[i] = children[i];
            }
            for (int i = 0; i < mergedChildren.Count; i++)
            {
                combined[children.Count - 1 + i] = mergedChildren[i];
            }
            return Node<TInfo, TLeaf, TLeafOps>.CreateInternal(combined);
        }

        throw new InvalidOperationException(
            $"Unexpected height relationship during concatenation: " +
            $"left={left.Height}, right={right.Height}, merged={merged.Height}");
    }

    /// <summary>
    /// 将字符串转换为 TLeaf 类型
    /// 对于 TLeaf = string，这是恒等转换
    /// 对于其他类型，需要通过 TLeafOps 提供转换逻辑
    /// </summary>
    private static TLeaf ConvertStringToLeaf(string segment)
    {
        // 这里使用类型检查来避免泛型约束污染
        // M4 完整实现时应通过 ILeafOperations 提供 FromString 方法
        if (typeof(TLeaf) == typeof(string))
        {
            return (TLeaf)(object)segment;
        }

        throw new NotSupportedException(
            $"GenericTreeBuilder.PushString only supports TLeaf = string. " +
            $"For custom leaf types, use PushNode with pre-constructed leaves.");
    }
}

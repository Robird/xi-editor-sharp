using System;
using Xi.Core.Rope;
using Xi.Core.Rope.Tree;

namespace Xi.Core.Tests;

/// <summary>
/// M3 架构管控措施 6：验证 TreeBuilder 可接受泛型节点
/// 目标：确保泛型 Node 接口兼容性，为 M4 完整切换铺路
/// </summary>
public class GenericNodeInterfaceTests
{
    private static Node<RopeInfo, string, StringLeafOperations> GenericLeaf(string text)
        => Node<RopeInfo, string, StringLeafOperations>.FromLeaf(text);

    [Fact]
    public void GenericTreeBuilder_Can_Build_From_Generic_Nodes()
    {
        // 创建泛型 TreeBuilder（M3 新增）
        var builder = new GenericTreeBuilder<RopeInfo, string, StringLeafOperations>();
        
        // 推入泛型节点
        builder.PushNode(GenericLeaf("Hello"));
        builder.PushNode(GenericLeaf(" "));
        builder.PushNode(GenericLeaf("World"));
        
        var result = builder.Build();
        
        // 验证聚合正确
        Assert.Equal(11, result.Length);
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void GenericTreeBuilder_PushString_Creates_Leaves_Via_LeafOperations()
    {
        var builder = new GenericTreeBuilder<RopeInfo, string, StringLeafOperations>();
        
        // 通过字符串接口推入（内部调用 StringLeafOperations）
        builder.PushString("Test\nContent");
        
        var result = builder.Build();
        
        Assert.Equal(12, result.Length);
        Assert.Equal(1, result.Info.LineCount); // LineCount = 换行符数量（\n 的个数）
    }

    [Fact]
    public void GenericTreeBuilder_Handles_Empty_Inputs()
    {
        var builder = new GenericTreeBuilder<RopeInfo, string, StringLeafOperations>();
        
        builder.PushString("");
        builder.PushNode(Node<RopeInfo, string, StringLeafOperations>.Empty);
        
        var result = builder.Build();
        
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void GenericTreeBuilder_Maintains_Height_Invariants()
    {
        var builder = new GenericTreeBuilder<RopeInfo, string, StringLeafOperations>();
        
        // 推入大量叶片触发多层合并
        for (int i = 0; i < 10; i++)
        {
            builder.PushString(new string('x', 600)); // 每个叶片 600 字符
        }
        
        var result = builder.Build();
        
        // 验证树高度合理（不会退化为链表）
        Assert.True(result.Height > 0);
        Assert.True(result.Height <= 4); // 10 个叶片，高度应在 1-3 之间
        Assert.Equal(6000, result.Length);
    }

    [Fact]
    public void GenericTreeBuilder_Reset_Clears_State()
    {
        var builder = new GenericTreeBuilder<RopeInfo, string, StringLeafOperations>();
        
        builder.PushString("First");
        builder.Reset();
        builder.PushString("Second");
        
        var result = builder.Build();
        
        // 验证只包含 Reset 后的内容
        Assert.Equal(6, result.Length);
    }

    [Fact]
    public void GenericNode_And_SpecializedNode_Are_Structurally_Compatible()
    {
        // 字符串特化路径
        var specialized = Node.FromLeaf("Test");
        
        // 泛型路径
        var generic = GenericLeaf("Test");
        
        // 验证核心属性对齐
        Assert.Equal(specialized.Length, generic.Length);
        Assert.Equal(specialized.IsLeaf, generic.IsLeaf);
        Assert.Equal(specialized.Height, generic.Height);
        
        // 验证 Info 聚合一致
        Assert.Equal(specialized.Info.LineCount, generic.Info.LineCount);
        Assert.Equal(specialized.Info.Utf16Length, generic.Info.Utf16Length);
    }

    [Fact]
    public void GenericTreeBuilder_Concat_Produces_Balanced_Tree()
    {
        var builder = new GenericTreeBuilder<RopeInfo, string, StringLeafOperations>();
        
        // 构建左子树
        builder.PushString(new string('a', 600));
        builder.PushString(new string('b', 600));
        var left = builder.Build();
        builder.Reset();
        
        // 构建右子树
        builder.PushString(new string('c', 600));
        builder.PushString(new string('d', 600));
        var right = builder.Build();
        builder.Reset();
        
        // 合并两棵树
        builder.PushNode(left);
        builder.PushNode(right);
        var merged = builder.Build();
        
        // 验证高度增加
        Assert.Equal(left.Height + 1, merged.Height);
        Assert.Equal(2400, merged.Length);
    }

    [Fact]
    public void GenericNode_TraverseLeaves_Returns_All_Leaf_Payloads()
    {
        var builder = new GenericTreeBuilder<RopeInfo, string, StringLeafOperations>();
        builder.PushString("Hello");
        builder.PushString(" ");
        builder.PushString("World");
        
        var root = builder.Build();
        
        // 验证遍历返回所有叶片
        var leaves = root.TraverseLeaves().ToList();
        Assert.Equal(3, leaves.Count);
        
        // 验证叶片内容（泛型节点的 Leaf 属性）
        var texts = leaves.Select(n => n.Leaf).ToList();
        Assert.Contains("Hello", texts);
        Assert.Contains(" ", texts);
        Assert.Contains("World", texts);
    }
}

using Xi.Core.Rope.Tree;

namespace Xi.Core.Tests;

public class StringLeafOperationsTests
{
    [Fact]
    public void Clone_Returns_New_Instance_With_Same_Content()
    {
        const string source = "hello world";

        var clone = StringLeafOperations.Clone(source);

        Assert.Equal(source, clone);
        Assert.NotSame(source, clone);
    }

    [Fact]
    public void Insert_Appends_Text_When_Index_Equals_Length()
    {
        const string source = "abc";
        const string addition = "123";

        var result = StringLeafOperations.Insert(source, source.Length, addition);

        Assert.Equal("abc123", result);
    }

    [Fact]
    public void Insert_Inserts_Text_In_Middle()
    {
        const string source = "abcdef";
        const string addition = "XYZ";

        var result = StringLeafOperations.Insert(source, 3, addition);

        Assert.Equal("abcXYZdef", result);
    }

    [Fact]
    public void RemoveRange_Removes_Requested_Segment()
    {
        const string source = "abcdefgh";

        var result = StringLeafOperations.RemoveRange(source, 2, 4);

        Assert.Equal("abgh", result);
    }

    [Fact]
    public void RemoveRange_Returns_Empty_When_All_Removed()
    {
        const string source = "123";

        var result = StringLeafOperations.RemoveRange(source, 0, source.Length);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ReplaceRange_Replaces_And_Preserves_Remaining_Text()
    {
        const string source = "abcdef";
        const string replacement = "XYZ";

        var result = StringLeafOperations.ReplaceRange(source, 1, 3, replacement);

        Assert.Equal("aXYZef", result);
    }

    [Fact]
    public void ReplaceRange_With_Empty_Acts_As_Delete()
    {
        const string source = "abcdef";

        var result = StringLeafOperations.ReplaceRange(source, 2, 2, string.Empty);

        Assert.Equal("abef", result);
    }

    [Fact]
    public void ReplaceRange_With_Length_Zero_Acts_As_Insert()
    {
        const string source = "abcdef";
        const string addition = "XYZ";

        var result = StringLeafOperations.ReplaceRange(source, 3, 0, addition);

        Assert.Equal("abcXYZdef", result);
    }
}

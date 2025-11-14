using System;
using System.Collections.Generic;

namespace Xi.Core.Rope;

internal enum DeltaElementKind
{
    Copy,
    Insert
}

internal readonly struct CopyElement
{
    public CopyElement(int start, int end)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, "Copy start must be non-negative.");
        }

        if (end < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(end), end, "Copy end must be non-negative.");
        }

        if (end <= start)
        {
            throw new ArgumentException("Copy end must be greater than copy start.", nameof(end));
        }

        Start = start;
        End = end;
    }

    public int Start { get; }

    public int End { get; }

    public int Length => End - Start;
}

internal readonly struct InsertElement<TLeaf>
    where TLeaf : class
{
    public InsertElement(TLeaf value, int length)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));

        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Insert length must be positive.");
        }

        if (value is string str && str.Length == 0)
        {
            throw new ArgumentException("Insert payload must not be empty.", nameof(value));
        }

        Length = length;
    }

    public TLeaf Value { get; }

    public int Length { get; }
}

internal readonly struct DeltaElement<TLeaf>
    where TLeaf : class
{
    private readonly DeltaElementKind _kind;
    private readonly CopyElement _copy;
    private readonly InsertElement<TLeaf> _insert;

    private DeltaElement(CopyElement copy)
    {
        _kind = DeltaElementKind.Copy;
        _copy = copy;
        _insert = default;
    }

    private DeltaElement(InsertElement<TLeaf> insert)
    {
        _kind = DeltaElementKind.Insert;
        _copy = default;
        _insert = insert;
    }

    internal static DeltaElement<TLeaf> Copy(int start, int end)
    {
        return new DeltaElement<TLeaf>(new CopyElement(start, end));
    }

    internal static DeltaElement<TLeaf> Insert(TLeaf value, int? length = null)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var resolvedLength = length ?? ResolveInsertLength(value);
        return new DeltaElement<TLeaf>(new InsertElement<TLeaf>(value, resolvedLength));
    }

    private static int ResolveInsertLength(TLeaf value)
    {
        if (value is string str)
        {
            if (str.Length == 0)
            {
                throw new ArgumentException("Insert payload must not be empty.", nameof(value));
            }

            return str.Length;
        }

        throw new NotSupportedException("An explicit insert length is required for non-string leaf types.");
    }

    internal DeltaElementKind Kind => _kind;

    internal bool IsCopy => _kind == DeltaElementKind.Copy;

    internal bool IsInsert => _kind == DeltaElementKind.Insert;

    internal CopyElement AsCopy()
    {
        if (!IsCopy)
        {
            throw new InvalidOperationException("Delta element is not a copy segment.");
        }

        return _copy;
    }

    internal InsertElement<TLeaf> AsInsert()
    {
        if (!IsInsert)
        {
            throw new InvalidOperationException("Delta element is not an insert segment.");
        }

        return _insert;
    }
}

internal sealed class Delta<TInfo, TLeaf>
    where TLeaf : class
{
    private readonly List<DeltaElement<TLeaf>> _elements;

    private Delta(List<DeltaElement<TLeaf>> elements, int baseLength)
    {
        _elements = elements;
        BaseLength = baseLength;
    }

    internal int BaseLength { get; }

    internal int ElementCount => _elements.Count;

    internal IReadOnlyList<DeltaElement<TLeaf>> Elements => _elements;

    internal IEnumerable<DeltaElement<TLeaf>> EnumerateElements()
    {
        return _elements;
    }

    internal IEnumerable<(bool IsInsert, int Start, int End)> EnumerateElementTriples()
    {
        var newOffset = 0;
        foreach (var element in _elements)
        {
            if (element.IsCopy)
            {
                var copy = element.AsCopy();
                newOffset += copy.Length;
                yield return (false, copy.Start, copy.End);
            }
            else
            {
                var insert = element.AsInsert();
                var start = newOffset;
                newOffset += insert.Length;
                yield return (true, start, newOffset);
            }
        }
    }

    internal static Delta<TInfo, TLeaf> FromElements(
        int baseLength,
        IEnumerable<DeltaElement<TLeaf>> elements)
    {
        if (elements == null)
        {
            throw new ArgumentNullException(nameof(elements));
        }

        var materialized = new List<DeltaElement<TLeaf>>();
        var maxCopyEnd = 0;

        foreach (var element in elements)
        {
            if (element.IsCopy)
            {
                var copy = element.AsCopy();
                if (copy.End > maxCopyEnd)
                {
                    maxCopyEnd = copy.End;
                }
            }
            else
            {
                _ = element.AsInsert();
            }

            materialized.Add(element);
        }

        ValidateBaseLength(baseLength, maxCopyEnd);

        return new Delta<TInfo, TLeaf>(materialized, baseLength);
    }

    internal static Delta<TInfo, TLeaf> FromElements(
        int baseLength,
        IEnumerable<(int? CopyStart, int? CopyEnd, TLeaf? Insert)> elementTuples)
    {
        if (elementTuples == null)
        {
            throw new ArgumentNullException(nameof(elementTuples));
        }

        var elements = new List<DeltaElement<TLeaf>>();
        foreach (var (copyStart, copyEnd, insertValue) in elementTuples)
        {
            var hasCopy = copyStart.HasValue || copyEnd.HasValue;
            var hasInsert = insertValue != null;

            if (hasCopy == hasInsert)
            {
                throw new ArgumentException("Each tuple must describe either a copy range or an insert value.", nameof(elementTuples));
            }

            if (hasCopy)
            {
                if (!copyStart.HasValue || !copyEnd.HasValue)
                {
                    throw new ArgumentException("Copy tuples must supply both start and end offsets.", nameof(elementTuples));
                }

                elements.Add(DeltaElement<TLeaf>.Copy(copyStart.Value, copyEnd.Value));
            }
            else
            {
                elements.Add(DeltaElement<TLeaf>.Insert(insertValue!));
            }
        }

        return FromElements(baseLength, elements);
    }

    internal (Delta<TInfo, TLeaf> InsertDelta, Subset DeletedSubset) Factor()
    {
        throw new NotImplementedException("Delta factorization is not implemented yet.");
    }

    private static void ValidateBaseLength(int baseLength, int maxCopyEnd)
    {
        if (baseLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseLength), baseLength, "Base length must be non-negative.");
        }

        if (maxCopyEnd > baseLength)
        {
            throw new ArgumentException("Copy elements must not reference offsets beyond the base length.", nameof(baseLength));
        }
    }
}

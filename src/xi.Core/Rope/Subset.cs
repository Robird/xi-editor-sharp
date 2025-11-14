using System;
using System.Collections.Generic;

namespace Xi.Core.Rope;

internal readonly struct SubsetSegment
{
    public SubsetSegment(int length, int count)
    {
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Segment length must be positive.");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Segment count must be non-negative.");
        }

        Length = length;
        Count = count;
    }

    public int Length { get; }

    public int Count { get; }
}

internal sealed class Subset
{
    private readonly SubsetSegment[] _segments;
    private readonly int _totalLength;

    private Subset(SubsetSegment[] segments, int totalLength)
    {
        _segments = segments;
        _totalLength = totalLength;
    }

    internal static Subset Create(SubsetSegment[] segments, int totalLength)
    {
        if (segments == null)
        {
            throw new ArgumentNullException(nameof(segments));
        }

        if (totalLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalLength));
        }

        var lengthSum = 0;
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            lengthSum += segment.Length;

            if (i == 0)
            {
                continue;
            }

            var previous = segments[i - 1];
            if (previous.Count == segment.Count)
            {
                throw new ArgumentException("Adjacent segments must have different counts.", nameof(segments));
            }
        }

        if (segments.Length == 0 && totalLength != 0)
        {
            throw new ArgumentException("Total length must be zero when no segments are present.", nameof(totalLength));
        }

        if (segments.Length > 0 && lengthSum != totalLength)
        {
            throw new ArgumentException("Total length must equal the sum of segment lengths.", nameof(totalLength));
        }

        return new Subset(segments, totalLength);
    }

    internal static Subset Empty { get; } = new(Array.Empty<SubsetSegment>(), 0);

    internal int SegmentCount => _segments.Length;

    internal bool IsEmpty => _segments.Length == 0 || (_segments.Length == 1 && _segments[0].Count == 0);

    internal int Length => _totalLength;

    internal int LengthAfterDelete()
    {
        var total = 0;
        for (var i = 0; i < _segments.Length; i++)
        {
            var segment = _segments[i];
            if (segment.Count == 0)
            {
                total += segment.Length;
            }
        }

        return total;
    }

    internal IEnumerable<(int Start, int Length, int Count)> SegmentTriples()
    {
        var offset = 0;
        for (var i = 0; i < _segments.Length; i++)
        {
            var segment = _segments[i];
            yield return (offset, segment.Length, segment.Count);
            offset += segment.Length;
        }
    }

    internal static Subset FromSegmentTriples(IEnumerable<(int Start, int Length, int Count)> triples)
    {
        if (triples == null)
        {
            throw new ArgumentNullException(nameof(triples));
        }

        var builder = new SubsetBuilder();
        var expectedStart = 0;
        foreach (var triple in triples)
        {
            var (start, length, count) = triple;
            if (length <= 0)
            {
                throw new ArgumentException("Segment length must be positive.", nameof(triples));
            }

            if (count < 0)
            {
                throw new ArgumentException("Segment count must be non-negative.", nameof(triples));
            }

            if (start < expectedStart)
            {
                throw new ArgumentException("Segments must be supplied in non-decreasing order.", nameof(triples));
            }

            if (start > expectedStart)
            {
                builder.PushSegment(start - expectedStart, 0);
            }

            builder.PushSegment(length, count);
            expectedStart = start + length;
        }

        return builder.Build();
    }
}

internal sealed class SubsetBuilder
{
    private readonly List<SubsetSegment> _segments = new();
    private int _totalLength;

    internal void PadToLength(int totalLength)
    {
        if (totalLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalLength));
        }

        if (totalLength < _totalLength)
        {
            throw new InvalidOperationException("Cannot pad to a length smaller than the current total.");
        }

        if (totalLength == _totalLength)
        {
            return;
        }

        PushSegmentInternal(totalLength - _totalLength, 0);
    }

    internal void AddRange(int begin, int end, int count)
    {
        if (begin < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(begin));
        }

        if (end < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(end));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (end < begin)
        {
            throw new ArgumentException("Range end must be greater than or equal to range begin.", nameof(end));
        }

        if (begin < _totalLength)
        {
            throw new InvalidOperationException("Ranges must be added in non-decreasing order.");
        }

        if (begin == end)
        {
            return;
        }

        if (begin > _totalLength)
        {
            PushSegmentInternal(begin - _totalLength, 0);
        }

        PushSegmentInternal(end - begin, count);
    }

    internal void PushSegment(int length, int count)
    {
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Segment length must be positive.");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Segment count must be non-negative.");
        }

        PushSegmentInternal(length, count);
    }

    private void PushSegmentInternal(int length, int count)
    {
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Segment length must be positive.");
        }

        _totalLength += length;

        if (_segments.Count > 0)
        {
            var lastIndex = _segments.Count - 1;
            var last = _segments[lastIndex];
            if (last.Count == count)
            {
                _segments[lastIndex] = new SubsetSegment(last.Length + length, count);
                return;
            }
        }

        _segments.Add(new SubsetSegment(length, count));
    }

    internal Subset Build()
    {
        if (_segments.Count == 0)
        {
            return Subset.Empty;
        }

        var copy = _segments.ToArray();
        return Subset.Create(copy, _totalLength);
    }
}

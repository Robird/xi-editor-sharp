using System;

namespace Xi.Core.Rope;

/// <summary>
/// Closed-open interval [Start, End) used throughout the rope data structures.
/// Mirrors xi-editor's <c>Interval</c> type and keeps the start &lt;= end invariant.
/// </summary>
public readonly struct Interval : IEquatable<Interval>
{
    public Interval(int start, int end)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, "Start must be non-negative.");
        }

        if (end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(end), end, "End must be greater than or equal to start.");
        }

        Start = start;
        End = end;
    }

    /// <summary>The inclusive start of the interval.</summary>
    public int Start { get; }

    /// <summary>The exclusive end of the interval.</summary>
    public int End { get; }

    /// <summary>Gets the length of the interval.</summary>
    public int Length => End - Start;

    /// <summary>Returns <c>true</c> when the interval contains no elements.</summary>
    public bool IsEmpty => Start == End;

    /// <summary>Determines whether the provided coordinate lies within the interval.</summary>
    public bool Contains(int position) => position >= Start && position < End;

    /// <summary>Creates a new interval representing the intersection of this interval with <paramref name="other"/>.</summary>
    public Interval Intersect(Interval other)
    {
        var start = Math.Max(Start, other.Start);
        var end = Math.Min(End, other.End);
        return end <= start ? Empty : new Interval(start, end);
    }

    /// <summary>Creates a new interval encompassing both this interval and <paramref name="other"/>.</summary>
    public Interval Union(Interval other)
    {
        if (IsEmpty)
        {
            return other;
        }

        if (other.IsEmpty)
        {
            return this;
        }

        return new Interval(Math.Min(Start, other.Start), Math.Max(End, other.End));
    }

    /// <summary>Translates the interval by <paramref name="delta"/> units.</summary>
    public Interval Translate(int delta)
    {
        if (delta == 0)
        {
            return this;
        }

        var newStart = checked(Start + delta);
        var newEnd = checked(End + delta);
        return new Interval(newStart, newEnd);
    }

    /// <summary>Creates an empty interval located at <paramref name="position"/>.</summary>
    public static Interval EmptyAt(int position) => new(position, position);

    /// <summary>An interval representing [0, 0).</summary>
    public static Interval Empty => new(0, 0);

    public bool Equals(Interval other) => Start == other.Start && End == other.End;

    public override bool Equals(object? obj) => obj is Interval other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public override string ToString() => $"[{Start}, {End})";

    public static bool operator ==(Interval left, Interval right) => left.Equals(right);

    public static bool operator !=(Interval left, Interval right) => !left.Equals(right);
}

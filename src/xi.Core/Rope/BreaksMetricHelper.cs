using System;

namespace Xi.Core.Rope;

/// <summary>
/// Provides allocation-free helpers for working with break metrics stored as sorted offsets.
/// </summary>
internal static class BreaksMetricHelper
{
    /// <summary>
    /// Returns the base-unit offset of the specified break index using 1-based measured units.
    /// </summary>
    /// <param name="breaks">Sorted break offsets measured in base units.</param>
    /// <param name="leafLength">The length of the leaf containing the breaks.</param>
    /// <param name="measuredUnits">The 1-based break count to resolve.</param>
    public static int GetNthBreakOffset(ReadOnlySpan<int> breaks, int leafLength, int measuredUnits)
    {
        if (measuredUnits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(measuredUnits));
        }

        if (leafLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(leafLength));
        }

        if (measuredUnits == 0)
        {
            return 0;
        }

        if (measuredUnits > breaks.Length)
        {
            return checked(leafLength + 1);
        }

        return breaks[measuredUnits - 1];
    }

    /// <summary>
    /// Counts break indices that are less than or equal to the specified offset.
    /// </summary>
    /// <param name="breaks">Sorted break offsets measured in base units.</param>
    /// <param name="offset">The inclusive search offset.</param>
    public static int CountBreaksUpTo(ReadOnlySpan<int> breaks, int offset)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        var low = 0;
        var high = breaks.Length;
        while (low < high)
        {
            var mid = low + ((high - low) >> 1);
            if (breaks[mid] <= offset)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }

    /// <summary>
    /// Finds the largest break strictly less than the specified offset.
    /// </summary>
    /// <param name="breaks">Sorted break offsets measured in base units.</param>
    /// <param name="offset">The exclusive upper bound.</param>
    public static int? FindPreviousBreak(ReadOnlySpan<int> breaks, int offset)
    {
        var low = 0;
        var high = breaks.Length;
        while (low < high)
        {
            var mid = low + ((high - low) >> 1);
            if (breaks[mid] < offset)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        if (low == 0)
        {
            return null;
        }

        return breaks[low - 1];
    }

    /// <summary>
    /// Finds the smallest break strictly greater than the specified offset.
    /// </summary>
    /// <param name="breaks">Sorted break offsets measured in base units.</param>
    /// <param name="offset">The exclusive lower bound.</param>
    public static int? FindNextBreak(ReadOnlySpan<int> breaks, int offset)
    {
        var low = 0;
        var high = breaks.Length;
        while (low < high)
        {
            var mid = low + ((high - low) >> 1);
            if (breaks[mid] <= offset)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        if (low >= breaks.Length)
        {
            return null;
        }

        return breaks[low];
    }

    /// <summary>
    /// Determines whether the specified offset matches a recorded break.
    /// </summary>
    /// <param name="breaks">Sorted break offsets measured in base units.</param>
    /// <param name="offset">The offset to check.</param>
    public static bool IsBreakBoundary(ReadOnlySpan<int> breaks, int offset)
    {
        var low = 0;
        var high = breaks.Length - 1;
        while (low <= high)
        {
            var mid = low + ((high - low) >> 1);
            var value = breaks[mid];
            if (value == offset)
            {
                return true;
            }

            if (value < offset)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return false;
    }
}

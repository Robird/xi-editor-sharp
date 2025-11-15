using System.Threading;

namespace Xi.Core.Rope.Navigation;

/// <summary>
/// Thread-safe counters used by grapheme navigation to surface degraded-path decisions.
/// </summary>
public sealed class GraphemeNavigationMetrics
{
    private long _forwardNeighborRequests;
    private long _backwardNeighborRequests;
    private long _scalarFallbacks;
    private long _moveNextCalls;
    private long _movePreviousCalls;

    public void RecordNeighborRequest(bool forward)
    {
        if (forward)
        {
            Interlocked.Increment(ref _forwardNeighborRequests);
        }
        else
        {
            Interlocked.Increment(ref _backwardNeighborRequests);
        }
    }

    public void RecordScalarFallback()
    {
        Interlocked.Increment(ref _scalarFallbacks);
    }

    public void RecordMoveNext() => Interlocked.Increment(ref _moveNextCalls);

    public void RecordMovePrevious() => Interlocked.Increment(ref _movePreviousCalls);

    /// <summary>Resets all counters to zero so parity tests can gather per-descriptor telemetry.</summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _forwardNeighborRequests, 0);
        Interlocked.Exchange(ref _backwardNeighborRequests, 0);
        Interlocked.Exchange(ref _scalarFallbacks, 0);
        Interlocked.Exchange(ref _moveNextCalls, 0);
        Interlocked.Exchange(ref _movePreviousCalls, 0);
    }

    /// <summary>Returns a stable snapshot of the telemetry counters.</summary>
    public Snapshot GetSnapshot() => new(
        ForwardNeighborRequests: Interlocked.Read(ref _forwardNeighborRequests),
        BackwardNeighborRequests: Interlocked.Read(ref _backwardNeighborRequests),
        ScalarFallbacks: Interlocked.Read(ref _scalarFallbacks),
        MoveNextCalls: Interlocked.Read(ref _moveNextCalls),
        MovePreviousCalls: Interlocked.Read(ref _movePreviousCalls));

    public readonly record struct Snapshot(
        long ForwardNeighborRequests,
        long BackwardNeighborRequests,
        long ScalarFallbacks,
        long MoveNextCalls,
        long MovePreviousCalls);
}

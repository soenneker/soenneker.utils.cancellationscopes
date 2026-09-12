using Soenneker.Utils.CancellationScopes.Abstract;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.CancellationScopes;

public sealed class CancellationScope : ICancellationScope
{
    private static readonly CancellationGeneration _disposed = new(null);
    private readonly CancellationToken _linkedToken;
    private CancellationGeneration? _current;

    public CancellationScope() : this(CancellationToken.None)
    {
    }

    public CancellationScope(CancellationToken linkedToken)
    {
        _linkedToken = linkedToken;
    }

    public CancellationToken CancellationToken => (Volatile.Read(ref _current) ?? Initialize()).Token;

    private CancellationGeneration Initialize()
    {
        CancellationGeneration created = CreateGeneration();
        CancellationGeneration? existing = Interlocked.CompareExchange(ref _current, created, null);
        if (existing is null)
            return created;

        // This candidate was never exposed. It only needs to release its parent
        // registration; no application can have registered on its token.
        created.Source!.Dispose();
        return existing;
    }

    public void Cancel()
    {
        CancellationTokenSource? source = Volatile.Read(ref _current)?.Source;
        if (source is null || source.IsCancellationRequested)
            return;

        try
        {
            source.Cancel();
        }
        catch
        {
            // Preserve best-effort cancellation, including concurrent teardown
            // and exceptions from application cancellation callbacks.
        }
    }

    public ValueTask ResetCancellation()
    {
        CancellationGeneration? previous = Volatile.Read(ref _current);
        if (ReferenceEquals(previous, _disposed))
            return ValueTask.CompletedTask;

        CancellationGeneration created = CreateGeneration();
        while (true)
        {
            CancellationGeneration? observed = Interlocked.CompareExchange(ref _current, created, previous);
            if (ReferenceEquals(observed, previous))
                return previous is null ? ValueTask.CompletedTask : CancellationTeardown.Run(previous.Source!);

            previous = observed;
            if (ReferenceEquals(previous, _disposed))
            {
                created.Source!.Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        CancellationGeneration? previous = Interlocked.Exchange(ref _current, _disposed);
        return previous?.Source is { } source ? CancellationTeardown.Run(source) : ValueTask.CompletedTask;
    }

    private CancellationGeneration CreateGeneration() => new(_linkedToken.CanBeCanceled
        ? CancellationTokenSource.CreateLinkedTokenSource(_linkedToken)
        : new CancellationTokenSource());
}

using Soenneker.Utils.AtomicResources;
using Soenneker.Utils.CancellationScopes.Abstract;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.CancellationScopes;

///<inheritdoc cref="ICancellationScope"/>
public sealed class CancellationScope : ICancellationScope
{
    private readonly AtomicResource<CancellationTokenSource> _atomic;
    private readonly CancellationToken _linkedToken;
    private readonly bool _link;

    public CancellationScope() : this(CancellationToken.None)
    {
    }

    /// <summary>Creates a scope whose CTS instances are linked to <paramref name="linkedToken"/>.</summary>
    public CancellationScope(CancellationToken linkedToken)
    {
        _linkedToken = linkedToken;
        _link = linkedToken.CanBeCanceled;

        _atomic = new AtomicResource<CancellationTokenSource>(
            factory: CreateCts,
            teardown: Teardown);
    }

    public CancellationToken CancellationToken => _atomic.GetOrCreate()!.Token;

    public void Cancel()
    {
        var cts = _atomic.TryGet();

        if (cts is null || cts.IsCancellationRequested)
            return;

        try
        {
            cts.Cancel();
        }
        catch
        {
            /* ignore */
        }
    }

    public ValueTask ResetCancellation() => _atomic.Reset();

    public ValueTask DisposeAsync() => _atomic.DisposeAsync();

    private CancellationTokenSource CreateCts()
        => _link ? CancellationTokenSource.CreateLinkedTokenSource(_linkedToken) : new CancellationTokenSource();

    private static ValueTask Teardown(CancellationTokenSource cts)
    {
        try
        {
            cts.Cancel();
        }
        catch
        {
            /* ignore */
        }

        cts.Dispose();
        return ValueTask.CompletedTask;
    }
}

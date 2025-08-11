using Soenneker.Utils.AtomicResources;
using Soenneker.Utils.CancellationScopes.Abstract;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Extensions.Task;

namespace Soenneker.Utils.CancellationScopes;

///<inheritdoc cref="ICancellationScope"/>
public sealed class CancellationScope : ICancellationScope
{
    private readonly AtomicResource<CancellationTokenSource> _atomic;

    public CancellationScope() : this(CancellationToken.None)
    {
    }

    /// <summary>Creates a scope whose CTS instances are linked to <paramref name="linkedToken"/>.</summary>
    public CancellationScope(CancellationToken linkedToken)
    {
        _atomic = new AtomicResource<CancellationTokenSource>(
            factory: () => linkedToken.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(linkedToken) : new CancellationTokenSource(),
            teardown: async cts =>
            {
                try
                {
                    await cts.CancelAsync().NoSync();
                }
                catch
                {
                    /* ignore */
                }

                cts.Dispose();
            });
    }

    public CancellationToken CancellationToken => _atomic.GetOrCreate()?.Token ?? CancellationToken.None;

    public Task Cancel()
    {
        CancellationTokenSource? cts = _atomic.TryGet();
        return cts is null ? Task.CompletedTask : cts.CancelAsync();
    }

    public ValueTask ResetCancellation() => _atomic.Reset();

    public ValueTask DisposeAsync() => _atomic.DisposeAsync();
}
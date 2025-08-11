using Soenneker.Utils.CancellationScopes.Abstract;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.CancellationScopes;

///<inheritdoc cref="ICancellationScope"/>
public sealed class CancellationScope : ICancellationScope
{
    private CancellationTokenSource? _cts;
    private volatile bool _disposed;

    public CancellationToken CancellationToken => _disposed ? CancellationToken.None : (_cts ??= new CancellationTokenSource()).Token;

    public Task Cancel() => _cts is null ? Task.CompletedTask : _cts.CancelAsync();

    public async ValueTask ResetCancellation()
    {
        if (_disposed)
            return;

        var fresh = new CancellationTokenSource();
        CancellationTokenSource? old = Interlocked.Exchange(ref _cts, fresh);

        if (old is not null)
        {
            try
            {
                await old.CancelAsync().ConfigureAwait(false);
            }
            catch
            {
                /* ignore */
            }

            old.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        CancellationTokenSource? cts = Interlocked.Exchange(ref _cts, null);

        if (cts is null)
            return;

        try
        {
            await cts.CancelAsync().ConfigureAwait(false);
        }
        catch
        {
            /* ignore */
        }

        cts.Dispose();
    }
}
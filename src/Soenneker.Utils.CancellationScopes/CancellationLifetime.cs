using Soenneker.Utils.CancellationScopes.Abstract;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.CancellationScopes;

public readonly struct CancellationLifetime : ICancellationLifetime
{
    private readonly CancellationTokenSource? _source;
    private readonly CancellationToken _token;

    public CancellationLifetime()
    {
        _source = new CancellationTokenSource();
        _token = _source.Token;
    }

    public CancellationLifetime(CancellationToken parent)
    {
        if (parent.IsCancellationRequested)
        {
            // This lifetime can never become live. No child source or parent
            // registration is needed, and the shared token does not retain parent.
            _source = null;
            _token = new CancellationToken(canceled: true);
            return;
        }

        _source = parent.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(parent) : new CancellationTokenSource();
        _token = _source.Token;
    }

    public CancellationToken CancellationToken => _token;

    public void Cancel()
    {
        if (_source is null || _token.IsCancellationRequested)
            return;

        try
        {
            _source.Cancel();
        }
        catch
        {
            // Match CancellationScope's best-effort callback handling.
        }
    }

    public ValueTask DisposeAsync() => _source is null ? ValueTask.CompletedTask : CancellationTeardown.Run(_source);
}

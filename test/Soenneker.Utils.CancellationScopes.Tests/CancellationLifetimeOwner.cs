using System;
using System.Threading.Tasks;

namespace Soenneker.Utils.CancellationScopes.Tests;

internal sealed class CancellationLifetimeOwner : IAsyncDisposable
{
    internal readonly CancellationLifetime Lifetime = new();
    public ValueTask DisposeAsync() => Lifetime.DisposeAsync();
}

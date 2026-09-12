using System.Threading;

namespace Soenneker.Utils.CancellationScopes;

internal sealed class CancellationGeneration(CancellationTokenSource? source)
{
    internal readonly CancellationTokenSource? Source = source;
    // Cache before publication: Source.Token can throw after concurrent disposal,
    // but a previously obtained token remains readable.
    internal readonly CancellationToken Token = source?.Token ?? default;
}

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.CancellationScopes.Abstract;

/// <summary>
/// Owns one cancellation token for an owner's entire lifetime, without reset support.
/// </summary>
/// <remarks>
/// <para>
/// Construct <see cref="CancellationLifetime"/> with <c>new()</c> to allocate its source eagerly.
/// The constructor accepting a parent token links that parent to the lifetime. If the parent
/// is already cancelled, it uses a shared cancelled token without allocating a child source.
/// A default value
/// has no source; cancellation and disposal of that value do nothing.
/// </para>
/// <para>
/// Copies share the same source and token. Assign one logical owner, and coordinate its
/// cancellation/disposal with operations using the token, as with a direct
/// <see cref="CancellationTokenSource"/>. Concurrent disposal is not supported.
/// Disposal does not await application operations or cancellation initiated elsewhere.
/// </para>
/// <para>
/// Store the concrete value type, including in readonly fields, to avoid a wrapper allocation.
/// Converting it to this interface or another interface boxes the value.
/// </para>
/// </remarks>
public interface ICancellationLifetime : IAsyncDisposable
{
    /// <summary>
    /// Gets the cached token. It remains readable and cancelled after disposal.
    /// </summary>
    /// <remarks>A default lifetime returns <see cref="CancellationToken.None"/>.</remarks>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Cancels the token synchronously, suppressing callback exceptions. Does not replace the token.
    /// </summary>
    void Cancel();

    /// <summary>
    /// Requests cancellation, awaits callbacks initiated by this call, and disposes the source.
    /// Cancellation callback exceptions are suppressed so owner cleanup can continue.
    /// </summary>
    /// <returns>The completion of cancellation and source disposal.</returns>
    new ValueTask DisposeAsync();
}

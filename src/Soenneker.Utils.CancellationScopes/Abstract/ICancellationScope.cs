using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.CancellationScopes.Abstract;

/// <summary>
/// Defines a contract for managing the lifecycle of a <see cref="CancellationToken"/> 
/// in a thread-safe and reusable manner.
/// </summary>
/// <remarks>
/// Implementations are responsible for creating, resetting, and canceling the underlying 
/// <see cref="CancellationTokenSource"/>. Once disposed, the scope should no longer 
/// allow new tokens to be created.
/// Cancellation callback exceptions are suppressed during cancellation and cleanup.
/// Consumers must still guard their own disposed state; a token is not an operation lease.
/// </remarks>
public interface ICancellationScope : IAsyncDisposable
{
    /// <summary>
    /// Gets the current <see cref="CancellationToken"/> for in-flight operations.
    /// </summary>
    /// <remarks>
    /// A new <see cref="CancellationTokenSource"/> will be lazily created if one 
    /// does not already exist. After disposal, this property should return 
    /// <see cref="CancellationToken.None"/>.
    /// A read overlapping reset or disposal may return the previous generation's token;
    /// that token remains readable and will be cancelled by its owning teardown.
    /// </remarks>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Cancels any in-flight work associated with the current <see cref="CancellationToken"/>.
    /// </summary>
    /// <remarks>
    /// This method is a no-op if no <see cref="CancellationTokenSource"/> 
    /// has been created yet. The underlying token source is not replaced; 
    /// subsequent calls to <see cref="CancellationToken"/> will return the 
    /// same (now canceled) token until <see cref="ResetCancellation"/> is invoked.
    /// </remarks>
    void Cancel();

    /// <summary>
    /// Cancels the current <see cref="CancellationToken"/> (if any) and replaces it 
    /// with a fresh <see cref="CancellationTokenSource"/> for new work.
    /// </summary>
    /// <remarks>
    /// This method allows a consumer to cleanly cancel in-progress work 
    /// and immediately prepare for a new operation without lingering cancellation state.
    /// After disposal, this method should perform no action.
    /// The replacement is published before cancellation callbacks for the old token run.
    /// Completion awaits the cancellation initiated by this reset, but does not await
    /// application operations using the old token or other concurrent resets.
    /// </remarks>
    /// <returns>Cancels the current <see cref="CancellationToken"/> (if any) and replaces it with a fresh <see cref="CancellationTokenSource"/> for new work.</returns>
    ValueTask ResetCancellation();
}

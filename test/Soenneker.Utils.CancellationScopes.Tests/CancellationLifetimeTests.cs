using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.CancellationScopes.Tests;

public sealed class CancellationLifetimeTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    [Test]
    public async Task Constructed_lifetime_has_one_token_that_stays_cancelled_after_disposal()
    {
        var lifetime = new CancellationLifetime();
        CancellationToken token = lifetime.CancellationToken;
        Check(token.CanBeCanceled && !token.IsCancellationRequested, "Constructor did not create a live token");
        Check(token == lifetime.CancellationToken, "Token changed between reads");
        await lifetime.DisposeAsync();
        Check(token.IsCancellationRequested && token == lifetime.CancellationToken, "Disposal lost the cached token");
        await lifetime.DisposeAsync();
        lifetime.Cancel();
    }

    [Test]
    public async Task Default_lifetime_is_an_empty_noop()
    {
        CancellationLifetime lifetime = default;
        Check(lifetime.CancellationToken == CancellationToken.None, "Default token must be None");
        lifetime.Cancel();
        await lifetime.DisposeAsync();
    }

    [Test]
    public async Task Copies_and_readonly_fields_share_the_same_cancellation()
    {
        var owner = new CancellationLifetimeOwner();
        CancellationLifetime copy = owner.Lifetime;
        Check(copy.CancellationToken == owner.Lifetime.CancellationToken, "Copy created a different source");
        copy.Cancel();
        Check(owner.Lifetime.CancellationToken.IsCancellationRequested, "Readonly field lost shared cancellation");
        await owner.DisposeAsync();
        await copy.DisposeAsync();
    }

    [Test]
    public async Task Parent_cancellation_propagates_and_child_cancellation_does_not_cancel_parent()
    {
        using var parent = new CancellationTokenSource();
        var child = new CancellationLifetime(parent.Token);
        child.Cancel();
        Check(!parent.IsCancellationRequested, "Child cancelled its parent");
        await child.DisposeAsync();
        var other = new CancellationLifetime(parent.Token);
        await parent.CancelAsync();
        Check(other.CancellationToken.IsCancellationRequested, "Parent cancellation did not propagate");
        await other.DisposeAsync();
        await using var alreadyCancelled = new CancellationLifetime(parent.Token);
        Check(alreadyCancelled.CancellationToken.IsCancellationRequested, "Cancelled parent produced a live child");
    }

    [Test]
    public async Task Already_cancelled_parent_needs_no_live_child_and_can_already_be_disposed()
    {
        var parent = new CancellationTokenSource();
        CancellationToken parentToken = parent.Token;
        await parent.CancelAsync();
        parent.Dispose();

        var lifetime = new CancellationLifetime(parentToken);
        CancellationToken token = lifetime.CancellationToken;
        Check(token.CanBeCanceled && token.IsCancellationRequested, "Cancelled parent produced a live lifetime");
        int callbacks = 0;
        using var registration = token.Register(() => callbacks++);
        Check(callbacks == 1, "Cancelled lifetime did not invoke a newly registered callback");
        lifetime.Cancel();
        await lifetime.DisposeAsync();
        await lifetime.DisposeAsync();
        Check(lifetime.CancellationToken == token && token.IsCancellationRequested, "Cleanup changed the lifetime token");
    }

    [Test]
    public async Task Dispose_awaits_callbacks_started_by_that_disposal()
    {
        var lifetime = new CancellationLifetime();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        using var registration = lifetime.CancellationToken.Register(() =>
        {
            entered.SetResult();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException();
        });
        Task disposing = lifetime.DisposeAsync().AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Check(lifetime.CancellationToken.IsCancellationRequested, "Disposal did not request cancellation immediately");
            Check(!disposing.IsCompleted, "Disposal did not wait for its callbacks");
        }
        finally { release.Set(); }
        await disposing.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task Callback_failures_do_not_prevent_cancellation_or_disposal()
    {
        var lifetime = new CancellationLifetime();
        using var registration = lifetime.CancellationToken.Register(() => throw new InvalidOperationException("callback"));
        await lifetime.DisposeAsync();
        Check(lifetime.CancellationToken.IsCancellationRequested, "Throwing callback prevented disposal");
        var other = new CancellationLifetime();
        using var otherRegistration = other.CancellationToken.Register(() => throw new InvalidOperationException("callback"));
        other.Cancel();
        await other.DisposeAsync();
    }
}

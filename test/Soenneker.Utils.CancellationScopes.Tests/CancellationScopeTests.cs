using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.CancellationScopes.Tests;

public sealed class CancellationScopeTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    [Test]
    public async Task Cancel_before_first_access_does_not_cancel_a_future_generation()
    {
        await using var scope = new CancellationScope();
        scope.Cancel();
        CancellationToken token = scope.CancellationToken;
        Check(token.CanBeCanceled && !token.IsCancellationRequested, "Cancel must preserve lazy initialization");
        Check(token == scope.CancellationToken, "Repeated access changed generation");
    }

    [Test]
    public async Task Cancel_keeps_the_same_token_until_reset()
    {
        await using var scope = new CancellationScope();
        CancellationToken first = scope.CancellationToken;
        scope.Cancel();
        Check(first.IsCancellationRequested && scope.CancellationToken == first, "Cancel replaced the token");
        await scope.ResetCancellation();
        CancellationToken second = scope.CancellationToken;
        Check(second != first && !second.IsCancellationRequested, "Reset failed to create a fresh token");
        Check(first.IsCancellationRequested, "Old token lost its cancellation state");
    }

    [Test]
    public async Task Reset_before_first_access_creates_a_generation()
    {
        await using var scope = new CancellationScope();
        await scope.ResetCancellation();
        scope.Cancel();
        Check(scope.CancellationToken.IsCancellationRequested, "Reset must eagerly create its replacement");
    }

    [Test]
    public async Task Each_generation_is_linked_to_the_parent()
    {
        using var parent = new CancellationTokenSource();
        await using var scope = new CancellationScope(parent.Token);
        CancellationToken first = scope.CancellationToken;
        await scope.ResetCancellation();
        CancellationToken second = scope.CancellationToken;
        await parent.CancelAsync();
        Check(first.IsCancellationRequested && second.IsCancellationRequested, "Parent link missing");
        await scope.ResetCancellation();
        Check(scope.CancellationToken.IsCancellationRequested, "Reset escaped a cancelled parent");
    }

    [Test]
    public async Task Disposed_scope_returns_none_and_cannot_be_recreated()
    {
        var scope = new CancellationScope();
        CancellationToken token = scope.CancellationToken;
        await scope.DisposeAsync();
        Check(token.IsCancellationRequested, "Disposal did not cancel the old token");
        await scope.ResetCancellation();
        scope.Cancel();
        await scope.DisposeAsync();
        Check(scope.CancellationToken == CancellationToken.None, "Disposed scope was resurrected");
        var unused = new CancellationScope();
        await unused.DisposeAsync();
        Check(unused.CancellationToken == CancellationToken.None, "Disposal initialized an unused scope");
    }

    [Test]
    public async Task Throwing_callbacks_do_not_prevent_reset_or_disposal()
    {
        var scope = new CancellationScope();
        using var first = scope.CancellationToken.Register(() => throw new InvalidOperationException("callback"));
        await scope.ResetCancellation();
        Check(!scope.CancellationToken.IsCancellationRequested, "Throwing callback prevented reset");
        using var second = scope.CancellationToken.Register(() => throw new InvalidOperationException("callback"));
        scope.Cancel();
        await scope.DisposeAsync();
        Check(scope.CancellationToken == CancellationToken.None, "Throwing callback prevented disposal");
    }

    [Test]
    public async Task Reset_publishes_replacement_before_waiting_for_old_callbacks()
    {
        await using var scope = new CancellationScope();
        CancellationToken first = scope.CancellationToken;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        using var registration = first.Register(() =>
        {
            entered.SetResult();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException();
        });
        Task resetting = scope.ResetCancellation().AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Check(!resetting.IsCompleted, "Reset did not await its callback");
            Check(scope.CancellationToken != first && !scope.CancellationToken.IsCancellationRequested, "Replacement was not published");
        }
        finally { release.Set(); }
        await resetting.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task Dispose_publishes_none_before_waiting_for_callbacks()
    {
        var scope = new CancellationScope();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        using var registration = scope.CancellationToken.Register(() =>
        {
            entered.SetResult();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException();
        });
        Task disposing = scope.DisposeAsync().AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Check(!disposing.IsCompleted, "Disposal did not await its callback");
            Check(scope.CancellationToken == CancellationToken.None, "Disposal did not close token publication");
            await scope.ResetCancellation();
        }
        finally { release.Set(); }
        await disposing.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task Cancellation_callbacks_can_reenter_the_scope()
    {
        await using var scope = new CancellationScope();
        CancellationToken observed = default;
        using var registration = scope.CancellationToken.Register(() => observed = scope.CancellationToken);
        await scope.ResetCancellation().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Check(observed == scope.CancellationToken && !observed.IsCancellationRequested, "Callback could not read replacement");
    }

    [Test]
    public async Task Concurrent_reads_resets_cancellation_and_disposal_do_not_throw_or_leak_live_tokens()
    {
        for (int iteration = 0; iteration < 20; iteration++)
        {
            var scope = new CancellationScope();
            var tokens = new ConcurrentBag<CancellationToken>();
            using var start = new ManualResetEventSlim();
            Task[] workers = Enumerable.Range(0, 4).Select(worker => Task.Run(async () =>
            {
                start.Wait();
                for (int i = 0; i < 500; i++)
                {
                    tokens.Add(scope.CancellationToken);
                    if (worker == 0) await scope.ResetCancellation();
                    if (worker == 1) scope.Cancel();
                    if (worker == 2 && i == 250) await scope.DisposeAsync();
                }
            })).ToArray();
            start.Set();
            await Task.WhenAll(workers).WaitAsync(TimeSpan.FromSeconds(10));
            await scope.DisposeAsync();
            Check(scope.CancellationToken == CancellationToken.None, "Disposed scope was resurrected");
            Check(tokens.All(token => !token.CanBeCanceled || token.IsCancellationRequested), "A published generation escaped cleanup");
        }
    }
}

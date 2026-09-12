using System.Diagnostics;
using Soenneker.Utils.CancellationScopes;

await using var baseline = new Baseline.CancellationScope();
await using var current = new CancellationScope();
using var parent = new CancellationTokenSource();
using var cancelledParent = new CancellationTokenSource();
cancelledParent.Cancel();
using var direct = new CancellationTokenSource();
CancellationToken cachedDirect = direct.Token;
await using var lifetime = new CancellationLifetime();
CancellationToken sink = default;
Measure("direct cached token", () => sink = cachedDirect);
Measure("lifetime cached token", () => sink = lifetime.CancellationToken);
Measure("old warm token", () => sink = baseline.CancellationToken);
Measure("new warm token", () => sink = current.CancellationToken);
Measure("old unused scope", () => new Baseline.CancellationScope().DisposeAsync().GetAwaiter().GetResult());
Measure("new unused scope", () => new CancellationScope().DisposeAsync().GetAwaiter().GetResult());
Measure("old used scope", () => { var scope = new Baseline.CancellationScope(); sink = scope.CancellationToken; scope.DisposeAsync().GetAwaiter().GetResult(); });
Measure("new used scope", () => { var scope = new CancellationScope(); sink = scope.CancellationToken; scope.DisposeAsync().GetAwaiter().GetResult(); });
Measure("old reset", () => baseline.ResetCancellation().GetAwaiter().GetResult());
Measure("new reset", () => current.ResetCancellation().GetAwaiter().GetResult());
Measure("old linked scope", () => { var scope = new Baseline.CancellationScope(parent.Token); sink = scope.CancellationToken; scope.DisposeAsync().GetAwaiter().GetResult(); });
Measure("new linked scope", () => { var scope = new CancellationScope(parent.Token); sink = scope.CancellationToken; scope.DisposeAsync().GetAwaiter().GetResult(); });
Measure("direct used lifetime", () => { var source = new CancellationTokenSource(); sink = source.Token; source.CancelAsync().GetAwaiter().GetResult(); source.Dispose(); });
Measure("direct service cleanup", () => { var source = new CancellationTokenSource(); sink = source.Token; DirectServiceCleanup(source).GetAwaiter().GetResult(); });
Measure("value used lifetime", () => { var value = new CancellationLifetime(); sink = value.CancellationToken; value.DisposeAsync().GetAwaiter().GetResult(); });
Measure("direct linked lifetime", () => { var source = CancellationTokenSource.CreateLinkedTokenSource(parent.Token); sink = source.Token; source.CancelAsync().GetAwaiter().GetResult(); source.Dispose(); });
Measure("value linked lifetime", () => { var value = new CancellationLifetime(parent.Token); sink = value.CancellationToken; value.DisposeAsync().GetAwaiter().GetResult(); });
Measure("direct cancelled parent", () => { var source = CancellationTokenSource.CreateLinkedTokenSource(cancelledParent.Token); sink = source.Token; source.Dispose(); });
Measure("value cancelled parent", () => { var value = new CancellationLifetime(cancelledParent.Token); sink = value.CancellationToken; value.DisposeAsync().GetAwaiter().GetResult(); });
GC.KeepAlive(sink);

// Matches the catch/finally cleanup currently used by the five interop services.
static async ValueTask DirectServiceCleanup(CancellationTokenSource source)
{
    try { await source.CancelAsync().ConfigureAwait(false); }
    catch { }
    finally { source.Dispose(); }
}

static void Measure(string name, Action action)
{
    const int count = 200_000;
    for (int i = 0; i < count; i++) action();
    double[] times = new double[5];
    long allocated = 0;
    for (int sample = 0; sample < times.Length; sample++)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        for (int i = 0; i < count; i++) action();
        times[sample] = Stopwatch.GetElapsedTime(start).TotalNanoseconds / count;
        allocated += GC.GetAllocatedBytesForCurrentThread() - before;
    }
    Array.Sort(times);
    Console.WriteLine($"{name,-22} {times[2],8:F1} ns/op {allocated / (double)(count * times.Length),8:F1} B/op (range {times[0]:F1}-{times[^1]:F1})");
}

using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.CancellationScopes;

internal static class CancellationTeardown
{
    internal static ValueTask Run(CancellationTokenSource source)
    {
        try
        {
            // CancelAsync also returns immediately when cancellation was already
            // requested. Skip it in that case to keep repeated disposal cheap.
            if (!source.IsCancellationRequested)
            {
                Task cancellation = source.CancelAsync();
                if (!cancellation.IsCompletedSuccessfully)
                    return AwaitCancellationAndDispose(cancellation, source);
            }
        }
        catch
        {
            // Cleanup must continue even if a callback throws.
        }

        source.Dispose();
        return ValueTask.CompletedTask;
    }

    private static async ValueTask AwaitCancellationAndDispose(Task cancellation, CancellationTokenSource source)
    {
        try
        {
            await cancellation.ConfigureAwait(false);
        }
        catch
        {
            // Cleanup must continue even if a callback throws.
        }
        finally
        {
            source.Dispose();
        }
    }
}

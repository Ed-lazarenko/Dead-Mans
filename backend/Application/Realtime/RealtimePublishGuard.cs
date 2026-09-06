namespace backend.Application.Realtime;
public static class RealtimePublishGuard
{
    private static readonly TimeSpan PublishTimeout = TimeSpan.FromSeconds(5);

    public static async Task TryPublishAsync(
        Func<CancellationToken, Task> publish,
        ILogger logger,
        string logTemplate,
        params object?[] logArgs
    )
    {
        using var timeout = new CancellationTokenSource(PublishTimeout);
        try
        {
            await publish(timeout.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, logTemplate, logArgs);
        }
    }
}

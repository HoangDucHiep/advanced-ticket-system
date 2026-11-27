using System.Diagnostics;

using Microsoft.Extensions.Logging;

using TicketSystem.Shared.Abstractions.Results;

namespace TicketSystem.Shared.Infrastructure.Application;

/// <summary>
/// Observation shared by the command and query decorators: one span, a start log, an end log with
/// duration and the error code on failure. Deliberately knows nothing about transactions (B7).
/// </summary>
internal static class HandlerInstrumentation
{
    public static async Task<Result<TResult>> RunAsync<TResult>(
        string handlerName,
        ILogger logger,
        Func<Task<Result<TResult>>> run
    )
    {
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity(handlerName);
        logger.LogDebug("Handling {Handler}", handlerName);
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            var result = await run();
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

            if (result.IsSuccess)
            {
                logger.LogInformation("Handled {Handler} in {ElapsedMs} ms", handlerName, elapsedMs);
            }
            else
            {
                activity?.SetTag("error.code", result.Error.Code);
                logger.LogWarning(
                    "Handled {Handler} in {ElapsedMs} ms with failure {ErrorCode}",
                    handlerName,
                    elapsedMs,
                    result.Error.Code);
            }

            return result;
        }
        catch (Exception exception)
        {
            // Logged once by the global exception handler; here we only mark the span.
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            throw;
        }
    }
}


using System.Diagnostics;

namespace TicketSystem.Shared.Infrastructure.Application;

/// <summary>Trace source for use-case handlers; OpenTelemetry subscribes to <see cref="ActivitySourceName"/>.</summary>
public static class ApplicationDiagnostics
{
    /// <summary>Name of the <see cref="ActivitySource"/> that wraps every handler run.</summary>
    public const string ActivitySourceName = "TicketSystem.Application";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}



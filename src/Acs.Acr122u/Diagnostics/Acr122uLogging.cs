namespace Acs.Acr122u.Diagnostics;

/// <summary>Severity of a <see cref="Acr122uLogEntry"/>.</summary>
public enum Acr122uLogLevel
{
    /// <summary>Highly detailed, per-command diagnostic output (raw APDU bytes in/out).</summary>
    Trace,

    /// <summary>Diagnostic output useful while developing against the reader.</summary>
    Debug,

    /// <summary>General informational messages about normal operation.</summary>
    Information,

    /// <summary>Something unexpected happened but the operation could still proceed.</summary>
    Warning,

    /// <summary>An operation failed.</summary>
    Error,
}

/// <summary>
/// A single diagnostic message emitted by the library. This library has no hard dependency on any
/// logging framework; wire <c>Acr122uReaderOptions.Logger</c> up to whatever logging
/// infrastructure your application already uses (Microsoft.Extensions.Logging, Serilog, a simple
/// <see cref="Console"/> writer, etc.).
/// </summary>
public readonly record struct Acr122uLogEntry(Acr122uLogLevel Level, string Message, Exception? Exception = null)
{
    /// <inheritdoc />
    public override string ToString() => Exception is null ? $"[{Level}] {Message}" : $"[{Level}] {Message} :: {Exception}";
}

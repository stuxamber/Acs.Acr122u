using Acs.Acr122u.Apdu;

namespace Acs.Acr122u.Exceptions;

/// <summary>
/// Thrown when the reader returns a non-success status word for a command. Carries the exact
/// command and response so callers can inspect <see cref="ResponseApdu.Sw1"/> /
/// <see cref="ResponseApdu.Sw2"/> against the "Response Codes" table for the failing operation.
/// </summary>
public sealed class Acr122uCommandException : Acr122uException
{
    /// <summary>The name of the high-level operation that failed (e.g. <c>nameof(ReadBinaryBlockAsync)</c>).</summary>
    public string OperationName { get; }

    /// <summary>The command APDU that was sent.</summary>
    public CommandApdu Command { get; }

    /// <summary>The response APDU that was returned.</summary>
    public ResponseApdu Response { get; }

    /// <summary>
    /// Initializes a new instance describing the failing operation, the exact command that was
    /// sent, and the status-word response the reader returned for it.
    /// </summary>
    public Acr122uCommandException(string operationName, CommandApdu command, ResponseApdu response)
        : base($"{operationName} failed with status word {response.Sw1:X2}{response.Sw2:X2}h.")
    {
        OperationName = operationName;
        Command = command;
        Response = response;
    }

    /// <summary>
    /// Standard <see cref="Exception"/> constructor, provided for consistency with .NET exception
    /// design guidelines. Prefer the primary constructor above, which also captures the failing
    /// command/response for diagnostics.
    /// </summary>
    public Acr122uCommandException()
        : base("The ACR122U rejected a command.")
    {
        OperationName = string.Empty;
    }

    /// <inheritdoc cref="Acr122uCommandException()" />
    public Acr122uCommandException(string message)
        : base(message)
    {
        OperationName = string.Empty;
    }

    /// <inheritdoc cref="Acr122uCommandException()" />
    public Acr122uCommandException(string message, Exception innerException)
        : base(message, innerException)
    {
        OperationName = string.Empty;
    }
}

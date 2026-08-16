using Acs.Acr122u.Exceptions;

namespace Acs.Acr122u.Transport;

/// <summary>Thrown when a WinSCard API call fails; carries the raw PC/SC return code for diagnostics.</summary>
public sealed class WinScardException : Acr122uTransportException
{
    /// <summary>The raw <c>SCARD_E_*</c> / <c>SCARD_W_*</c> return code from the failing API call.</summary>
    public int ScardErrorCode { get; }

    /// <summary>Initializes a new instance for the specified failing WinSCard API call and error code.</summary>
    public WinScardException(string apiName, int scardErrorCode)
        : base($"{apiName} failed with SCard error 0x{unchecked((uint)scardErrorCode):X8} ({Describe(scardErrorCode)}).")
    {
        ScardErrorCode = scardErrorCode;
    }

    /// <summary>
    /// Standard <see cref="Exception"/> constructor, provided for consistency with .NET exception
    /// design guidelines. Prefer the primary constructor above, which also captures the raw
    /// SCard error code for diagnostics.
    /// </summary>
    public WinScardException()
    {
    }

    /// <inheritdoc cref="WinScardException()" />
    public WinScardException(string message)
        : base(message)
    {
    }

    /// <inheritdoc cref="WinScardException()" />
    public WinScardException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    private static string Describe(int code) => unchecked((uint)code) switch
    {
        0x80100001 => "SCARD_E_INVALID_HANDLE",
        0x80100002 => "SCARD_E_INVALID_PARAMETER",
        0x80100003 => "SCARD_E_INVALID_TARGET",
        0x8010000B => "SCARD_E_SHARING_VIOLATION",
        0x8010000C => "SCARD_E_NO_SMARTCARD",
        0x8010000D => "SCARD_E_UNKNOWN_CARD",
        0x8010000F => "SCARD_E_PROTO_MISMATCH",
        0x80100010 => "SCARD_E_NOT_READY",
        0x8010001E => "SCARD_E_TIMEOUT",
        0x8010002E => "SCARD_E_NO_READERS_AVAILABLE",
        0x80100065 => "SCARD_W_UNSUPPORTED_CARD",
        0x80100066 => "SCARD_W_UNRESPONSIVE_CARD",
        0x80100067 => "SCARD_W_UNPOWERED_CARD",
        0x80100068 => "SCARD_W_RESET_CARD",
        0x80100069 => "SCARD_W_REMOVED_CARD",
        _ => "unrecognized SCard status — see the PC/SC SCARD_E_*/SCARD_W_* constants for the full list",
    };
}

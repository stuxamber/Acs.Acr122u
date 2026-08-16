using Acs.Acr122u.Apdu;

namespace Acs.Acr122u.Transport;

/// <summary>
/// Abstraction over the PC/SC transport used to talk to an ACR122U reader. The default,
/// <see cref="WinScardTransport"/>, talks to Windows' built-in WinSCard service using P/Invoke
/// with no external dependency. Implement this interface yourself (for example wrapping a
/// cross-platform PC/SC NuGet package) to run the rest of this library on Linux or macOS.
/// </summary>
public interface ISmartCardTransport : IAsyncDisposable, IDisposable
{
    /// <summary>The name of the connected reader, or <see langword="null"/> if not currently connected.</summary>
    string? ReaderName { get; }

    /// <summary>True once <see cref="ConnectAsync"/> has succeeded and <see cref="DisconnectAsync"/> has not yet been called.</summary>
    bool IsConnected { get; }

    /// <summary>Enumerates the names of every PC/SC reader currently attached to the system.</summary>
    IReadOnlyList<string> ListReaders();

    /// <summary>Connects to the given reader and powers up any inserted card.</summary>
    Task ConnectAsync(
        string readerName,
        SmartCardShareMode shareMode = SmartCardShareMode.Shared,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Polls the given reader at <paramref name="pollInterval"/> until a card is present, then
    /// connects to it. Optionally invokes <paramref name="onWaiting"/> before each wait (attempts
    /// are 1-based) so callers can surface progress, and optionally gives up with a
    /// <see cref="Exceptions.CardNotPresentException"/> once <paramref name="timeout"/> elapses
    /// with no card presented.
    /// </summary>
    Task ConnectWhenCardPresentAsync(
        string readerName,
        TimeSpan pollInterval,
        SmartCardShareMode shareMode = SmartCardShareMode.Shared,
        TimeSpan? timeout = null,
        Action<int>? onWaiting = null,
        CancellationToken cancellationToken = default);

    /// <summary>Disconnects from the reader.</summary>
    Task DisconnectAsync(
        SmartCardDisposition disposition = SmartCardDisposition.LeaveCard,
        CancellationToken cancellationToken = default);

    /// <summary>Sends a command APDU to the connected card and returns its response (PC/SC <c>SCardTransmit</c>).</summary>
    Task<ResponseApdu> TransmitAsync(CommandApdu command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a vendor "escape" command directly to the reader (PC/SC <c>SCardControl</c>),
    /// bypassing the card. Used for pseudo-APDUs (§6.0) when no card is connected. Requires the
    /// PC/SC escape command to be enabled for the reader driver — see Appendix A.
    /// </summary>
    Task<byte[]> ControlAsync(int controlCode, ReadOnlyMemory<byte> input, CancellationToken cancellationToken = default);

    /// <summary>Reads the ATR of the currently connected card.</summary>
    Task<byte[]> GetAtrAsync(CancellationToken cancellationToken = default);
}

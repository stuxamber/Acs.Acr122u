using System.Text;
using Acs.Acr122u.Apdu;
using Acs.Acr122u.Exceptions;

namespace Acs.Acr122u.Transport;

/// <summary>
/// A <see cref="ISmartCardTransport"/> implementation built directly on the Windows WinSCard API
/// via P/Invoke — no external NuGet dependency required. Use <see cref="Acr122uReaderFactory"/>
/// for the easiest way to enumerate readers and obtain a ready-to-use <see cref="Acr122uReader"/>.
/// </summary>
/// <remarks>This type is not thread-safe: use one instance per physical reader connection at a time.</remarks>
[SupportedOSPlatform("windows")]
public sealed class WinScardTransport : ISmartCardTransport
{
    private readonly ScardContextHandle _context;
    private ScardCardHandle? _card;
    private uint _activeProtocol;
    private bool _disposed;

    /// <summary>Establishes a new PC/SC resource-manager context.</summary>
    public WinScardTransport()
    {
        var result = NativeMethods.SCardEstablishContext(NativeMethods.ScardScopeUser, IntPtr.Zero, IntPtr.Zero, out var context);
        ThrowIfError(nameof(NativeMethods.SCardEstablishContext), result);
        _context = new ScardContextHandle(context);
    }

    /// <inheritdoc />
    public string? ReaderName { get; private set; }

    /// <inheritdoc />
    public bool IsConnected => _card is { IsInvalid: false, IsClosed: false };

    /// <inheritdoc />
    public IReadOnlyList<string> ListReaders()
    {
        var contextHandle = _context.DangerousGetHandle();

        var pcch = 0;
        var result = NativeMethods.SCardListReadersA(contextHandle, null, null, ref pcch);
        if (result == NativeMethods.ScardENoReadersAvailable)
        {
            return Array.Empty<string>();
        }

        ThrowIfError(nameof(NativeMethods.SCardListReadersA), result);

        var buffer = new byte[pcch];
        result = NativeMethods.SCardListReadersA(contextHandle, null, buffer, ref pcch);
        ThrowIfError(nameof(NativeMethods.SCardListReadersA), result);

        return ParseMultiString(buffer);
    }

    /// <inheritdoc />
    public Task ConnectAsync(
        string readerName,
        SmartCardShareMode shareMode = SmartCardShareMode.Shared,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(readerName);
        cancellationToken.ThrowIfCancellationRequested();

        var preferredProtocols = shareMode == SmartCardShareMode.Direct
            ? NativeMethods.ScardProtocolUndefined
            : NativeMethods.ScardProtocolAny;

        var result = NativeMethods.SCardConnectA(
            _context.DangerousGetHandle(),
            readerName,
            (uint)shareMode,
            preferredProtocols,
            out var cardHandle,
            out var activeProtocol);
        ThrowIfError(nameof(NativeMethods.SCardConnectA), result);

        _card?.Dispose();
        _card = new ScardCardHandle(cardHandle);
        _activeProtocol = activeProtocol;
        ReaderName = readerName;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ConnectWhenCardPresentAsync(
        string readerName,
        TimeSpan pollInterval,
        SmartCardShareMode shareMode = SmartCardShareMode.Shared,
        TimeSpan? timeout = null,
        Action<int>? onWaiting = null,
        CancellationToken cancellationToken = default)
    {
        if (pollInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pollInterval), pollInterval, "Poll interval must be positive.");
        }

        var deadline = timeout.HasValue ? DateTime.UtcNow + timeout.Value : (DateTime?)null;
        var attempt = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await ConnectAsync(readerName, shareMode, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (WinScardException ex) when (ex.ScardErrorCode is NativeMethods.ScardENoSmartcard or NativeMethods.ScardWRemovedCard)
            {
                attempt++;
                if (deadline.HasValue && DateTime.UtcNow >= deadline.Value)
                {
                    throw new CardNotPresentException(
                        $"No card was presented to '{readerName}' within {timeout}. " +
                        "Confirm a tag is actually sitting on the reader's antenna — a reader " +
                        "being attached over USB does not by itself mean a card is present.", ex);
                }

                // No tag is currently on the antenna; keep polling until one is presented or cancelled.
                onWaiting?.Invoke(attempt);
                await Task.Delay(pollInterval, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public Task DisconnectAsync(
        SmartCardDisposition disposition = SmartCardDisposition.LeaveCard,
        CancellationToken cancellationToken = default)
    {
        if (_card is null)
        {
            return Task.CompletedTask;
        }

        var result = NativeMethods.SCardDisconnect(_card.DangerousGetHandle(), (uint)disposition);
        _card.SetHandleAsInvalid();
        _card.Dispose();
        _card = null;
        ReaderName = null;

        ThrowIfError(nameof(NativeMethods.SCardDisconnect), result);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ResponseApdu> TransmitAsync(CommandApdu command, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        cancellationToken.ThrowIfCancellationRequested();

        var sendPci = new NativeMethods.ScardIoRequest
        {
            DwProtocol = _activeProtocol,
            CbPciLength = (uint)Marshal.SizeOf<NativeMethods.ScardIoRequest>(),
        };

        var send = command.ToByteArray();
        var recvBuffer = new byte[258]; // 256 data bytes + SW1 SW2 is the largest a short APDU response can be.
        var recvLength = recvBuffer.Length;

        var result = NativeMethods.SCardTransmit(
            _card!.DangerousGetHandle(), ref sendPci, send, send.Length, IntPtr.Zero, recvBuffer, ref recvLength);
        ThrowIfError(nameof(NativeMethods.SCardTransmit), result);

        return Task.FromResult(ResponseApdu.Parse(recvBuffer.AsSpan(0, recvLength)));
    }

    /// <inheritdoc />
    public Task<byte[]> ControlAsync(int controlCode, ReadOnlyMemory<byte> input, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_card is not { IsInvalid: false, IsClosed: false })
        {
            throw new Acr122uTransportException(
                "Not connected: connect (e.g. in Direct share mode) before sending escape commands, " +
                "and ensure the PC/SC escape command is enabled for the reader driver (see Appendix A).");
        }

        var inBuffer = input.ToArray();
        var outBuffer = new byte[258];

        var result = NativeMethods.SCardControl(
            _card.DangerousGetHandle(), NativeMethods.ScardCtlCode(controlCode), inBuffer, inBuffer.Length, outBuffer, outBuffer.Length, out var returned);
        ThrowIfError(nameof(NativeMethods.SCardControl), result);

        return Task.FromResult(outBuffer.AsSpan(0, returned).ToArray());
    }

    /// <inheritdoc />
    public Task<byte[]> GetAtrAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        cancellationToken.ThrowIfCancellationRequested();

        var atrBuffer = new byte[NativeMethods.MaxAtrSize];
        var atrLength = atrBuffer.Length;
        var readerLength = 0;

        var result = NativeMethods.SCardStatusA(
            _card!.DangerousGetHandle(), null, ref readerLength, out _, out _, atrBuffer, ref atrLength);
        ThrowIfError(nameof(NativeMethods.SCardStatusA), result);

        return Task.FromResult(atrBuffer.AsSpan(0, atrLength).ToArray());
    }

    private void EnsureConnected()
    {
        if (!IsConnected)
        {
            throw new CardNotPresentException("The transport is not connected to a reader/card. Call ConnectAsync first.");
        }
    }

    private static void ThrowIfError(string apiName, int result)
    {
        if (result != NativeMethods.ScardSSuccess)
        {
            throw new WinScardException(apiName, result);
        }
    }

    private static string[] ParseMultiString(ReadOnlySpan<byte> buffer)
    {
        // SCardListReaders returns a sequence of null-terminated ANSI strings, itself terminated by
        // an additional trailing null.
        var text = Encoding.ASCII.GetString(buffer);
        return text.Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _card?.Dispose();
        _context.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_card is not null)
        {
            await DisconnectAsync().ConfigureAwait(false);
        }

        Dispose();
    }
}

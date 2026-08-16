using System.Text;
using Acs.Acr122u.Apdu;
using Acs.Acr122u.Commands;
using Acs.Acr122u.Diagnostics;
using Acs.Acr122u.Exceptions;
using Acs.Acr122u.Models;
using Acs.Acr122u.Transport;

namespace Acs.Acr122u;

/// <summary>
/// A high-level, easy-to-use client for the ACS ACR122U USB NFC reader, implementing every
/// command documented in the ACR122U Application Programming Interface specification (v2.04).
/// </summary>
/// <remarks>
/// <para>
/// Create instances through <see cref="Acr122uReaderFactory"/> for the simplest experience, or
/// wrap your own <see cref="ISmartCardTransport"/> (for example a cross-platform PC/SC
/// implementation) and pass it to the public constructor for full control over connection
/// lifetime and sharing.
/// </para>
/// <para>
/// Every method throws <see cref="Acr122uCommandException"/> when the reader reports a failure
/// status word, and <see cref="CardNotPresentException"/> when no card is connected. Use
/// <see cref="TryAuthenticateAsync"/> where a failed authentication attempt is an expected,
/// non-exceptional outcome (e.g. probing several candidate keys).
/// </para>
/// <para>This type is not thread-safe: use one instance per physical reader connection at a time.</para>
/// </remarks>
public sealed class Acr122uReader : IAsyncDisposable, IDisposable
{
    private readonly ISmartCardTransport _transport;
    private readonly Acr122uReaderOptions _options;
    private readonly bool _ownsTransport;
    private bool _disposed;

    /// <summary>Wraps an already-constructed transport with the high-level ACR122U API.</summary>
    /// <param name="transport">The transport to use. Must already be connected, or be connected by the caller before use.</param>
    /// <param name="options">Optional behavioral settings.</param>
    /// <param name="ownsTransport">When <see langword="true"/>, disposing this reader also disposes <paramref name="transport"/>.</param>
    public Acr122uReader(ISmartCardTransport transport, Acr122uReaderOptions? options = null, bool ownsTransport = false)
    {
        ArgumentNullException.ThrowIfNull(transport);
        _transport = transport;
        _options = options ?? new Acr122uReaderOptions();
        _ownsTransport = ownsTransport;
    }

    /// <summary>The PC/SC reader name this instance is connected to, or <see langword="null"/> if not connected.</summary>
    public string? ReaderName => _transport.ReaderName;

    /// <summary>The underlying transport, exposed for advanced/raw APDU access.</summary>
    public ISmartCardTransport Transport => _transport;

    // ---------------------------------------------------------------------------------------------
    // §3.0 ATR / card identification
    // ---------------------------------------------------------------------------------------------

    /// <summary>Reads and parses the ATR of the currently connected card (§3.1).</summary>
    public async Task<AtrInfo> GetAtrInfoAsync(CancellationToken cancellationToken = default)
    {
        var atr = await _transport.GetAtrAsync(cancellationToken).ConfigureAwait(false);
        return AtrInfo.Parse(atr);
    }

    // ---------------------------------------------------------------------------------------------
    // §4.0 General purpose commands
    // ---------------------------------------------------------------------------------------------

    /// <summary>§4.1 Get Data — returns the UID of the connected PICC.</summary>
    public async Task<byte[]> GetUidAsync(CancellationToken cancellationToken = default)
    {
        var response = await ExecuteAsync(nameof(GetUidAsync), Acr122uCommands.GetUid(), cancellationToken).ConfigureAwait(false);
        return response.Data.ToArray();
    }

    /// <summary>§4.1 Get Data — returns the ATS of an ISO 14443-4 Type A PICC.</summary>
    public async Task<byte[]> GetAtsAsync(CancellationToken cancellationToken = default)
    {
        var response = await ExecuteAsync(nameof(GetAtsAsync), Acr122uCommands.GetAts(), cancellationToken).ConfigureAwait(false);
        return response.Data.ToArray();
    }

    // ---------------------------------------------------------------------------------------------
    // §5.0 MIFARE Classic commands
    // ---------------------------------------------------------------------------------------------

    /// <summary>§5.1 Loads an authentication key into the reader's volatile key store.</summary>
    public Task LoadAuthenticationKeyAsync(KeySlot slot, ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(LoadAuthenticationKeyAsync), Acr122uCommands.LoadAuthenticationKey(slot, key), cancellationToken);

    /// <summary>§5.2 Authenticates a block against a previously loaded key. Throws <see cref="Acr122uCommandException"/> on failure.</summary>
    public Task AuthenticateAsync(byte block, KeyType keyType, KeySlot slot, CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(AuthenticateAsync), Acr122uCommands.Authenticate(block, keyType, slot), cancellationToken);

    /// <summary>§5.2 Authenticates a block, returning <see langword="false"/> instead of throwing when authentication fails.</summary>
    public async Task<bool> TryAuthenticateAsync(byte block, KeyType keyType, KeySlot slot, CancellationToken cancellationToken = default)
    {
        var response = await _transport.TransmitAsync(Acr122uCommands.Authenticate(block, keyType, slot), cancellationToken)
            .ConfigureAwait(false);
        return response.IsSuccess;
    }

    /// <summary>Loads a key into <paramref name="slot"/> and immediately authenticates a block with it — a common two-step sequence.</summary>
    public async Task AuthenticateAsync(
        byte block, KeyType keyType, ReadOnlyMemory<byte> key, KeySlot slot = KeySlot.Slot0, CancellationToken cancellationToken = default)
    {
        await LoadAuthenticationKeyAsync(slot, key, cancellationToken).ConfigureAwait(false);
        await AuthenticateAsync(block, keyType, slot, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>§5.3 Reads up to 16 bytes from a block that has already been authenticated.</summary>
    public async Task<byte[]> ReadBinaryBlockAsync(byte block, byte length = 16, CancellationToken cancellationToken = default)
    {
        var response = await ExecuteAsync(nameof(ReadBinaryBlockAsync), Acr122uCommands.ReadBinaryBlock(block, length), cancellationToken)
            .ConfigureAwait(false);
        return response.Data.ToArray();
    }

    /// <summary>§5.4 Writes 4 (MIFARE Ultralight) or 16 (MIFARE Classic) bytes to a block that has already been authenticated.</summary>
    public Task UpdateBinaryBlockAsync(byte block, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(UpdateBinaryBlockAsync), Acr122uCommands.UpdateBinaryBlock(block, data), cancellationToken);

    // ---------------------------------------------------------------------------------------------
    // §5.5 Value block commands
    // ---------------------------------------------------------------------------------------------

    /// <summary>§5.5.1 Formats a block as a value block containing <paramref name="value"/>.</summary>
    public Task StoreValueAsync(byte block, int value, CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(StoreValueAsync), Acr122uCommands.WriteValueBlock(block, ValueBlockOperation.Store, value), cancellationToken);

    /// <summary>§5.5.1 Increments a value block by <paramref name="delta"/> (must be non-negative).</summary>
    public Task IncrementValueAsync(byte block, int delta, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(delta);
        return ExecuteAsync(
            nameof(IncrementValueAsync), Acr122uCommands.WriteValueBlock(block, ValueBlockOperation.Increment, delta), cancellationToken);
    }

    /// <summary>§5.5.1 Decrements a value block by <paramref name="delta"/> (must be non-negative).</summary>
    public Task DecrementValueAsync(byte block, int delta, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(delta);
        return ExecuteAsync(
            nameof(DecrementValueAsync), Acr122uCommands.WriteValueBlock(block, ValueBlockOperation.Decrement, delta), cancellationToken);
    }

    /// <summary>§5.5.2 Reads the signed 32-bit value stored in a value block.</summary>
    public async Task<int> ReadValueBlockAsync(byte block, CancellationToken cancellationToken = default)
    {
        var response = await ExecuteAsync(nameof(ReadValueBlockAsync), Acr122uCommands.ReadValueBlock(block), cancellationToken)
            .ConfigureAwait(false);
        return BinaryPrimitives.ReadInt32BigEndian(response.Data.Span);
    }

    /// <summary>§5.5.3 Copies a value from one value block to another block in the same sector.</summary>
    public Task RestoreValueBlockAsync(byte sourceBlock, byte targetBlock, CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(RestoreValueBlockAsync), Acr122uCommands.RestoreValueBlock(sourceBlock, targetBlock), cancellationToken);

    // ---------------------------------------------------------------------------------------------
    // §6.0 Pseudo-APDU / reader control commands
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// §6.1 Sends a raw payload directly to the tag or reader and returns the raw response,
    /// stripped of the reader's own trailing 90 00h wrapper. Callers are responsible for
    /// interpreting the tag/reader-specific framing themselves — see Appendices B and C.
    /// </summary>
    public async Task<byte[]> DirectTransmitAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        var response = await ExecuteAsync(nameof(DirectTransmitAsync), Acr122uCommands.DirectTransmit(payload), cancellationToken)
            .ConfigureAwait(false);
        return response.Data.ToArray();
    }

    /// <summary>§6.2 Controls the bi-color LED and buzzer.</summary>
    public async Task<LedBuzzerControlResult> SetLedAndBuzzerAsync(LedBuzzerControlRequest request, CancellationToken cancellationToken = default)
    {
        var response = await ExecuteAsync(nameof(SetLedAndBuzzerAsync), Acr122uCommands.SetLedAndBuzzer(request), cancellationToken)
            .ConfigureAwait(false);
        return LedBuzzerControlResult.FromStatusByte(response.Sw2);
    }

    /// <summary>§6.3 Returns the reader's firmware version string (e.g. <c>"ACR122U201"</c>).</summary>
    /// <remarks>
    /// Unlike every other response format table in the specification (which is explicitly
    /// "[data] + 2 bytes" for the SW1/SW2 trailer — see e.g. §4.1's "Get UID Response Format
    /// (UID + 2 bytes)"), §6.3's response format is documented as a bare "(10 bytes)": just the
    /// ASCII version string, with no status word appended at all. The worked example in the spec
    /// confirms this — <c>41 43 52 31 32 32 55 32 30 31h = ACR122U201 (ASCII)</c>, exactly 10 raw
    /// bytes. This method therefore does not go through the standard SW1-must-be-90h check that
    /// every other command uses; a successful <c>SCardTransmit</c> call is itself proof the
    /// exchange succeeded, and the entire captured response (our generic parser's Data plus the
    /// two bytes it otherwise treats as SW1/SW2) is the version string.
    /// </remarks>
    public async Task<string> GetFirmwareVersionAsync(CancellationToken cancellationToken = default)
    {
        var command = Acr122uCommands.GetFirmwareVersion();
        var response = await _transport.TransmitAsync(command, cancellationToken).ConfigureAwait(false);
        Log(Acr122uLogLevel.Trace, $"{nameof(GetFirmwareVersionAsync)}: {command} -> {response}");

        byte[] raw = [.. response.Data.ToArray(), response.Sw1, response.Sw2];
        return Encoding.ASCII.GetString(raw).TrimEnd('\0');
    }

    /// <summary>§6.4 Reads the reader's current PICC operating parameter flags.</summary>
    public async Task<PiccOperatingParameters> GetPiccOperatingParameterAsync(CancellationToken cancellationToken = default)
    {
        var command = Acr122uCommands.GetPiccOperatingParameter();
        var response = await _transport.TransmitAsync(command, cancellationToken).ConfigureAwait(false);
        ThrowIfNotAcknowledged(nameof(GetPiccOperatingParameterAsync), command, response);
        return (PiccOperatingParameters)response.Sw2;
    }

    /// <summary>§6.5 Sets the reader's PICC operating parameter flags.</summary>
    public async Task<PiccOperatingParameters> SetPiccOperatingParameterAsync(
        PiccOperatingParameters parameters, CancellationToken cancellationToken = default)
    {
        var command = Acr122uCommands.SetPiccOperatingParameter(parameters);
        var response = await _transport.TransmitAsync(command, cancellationToken).ConfigureAwait(false);
        ThrowIfNotAcknowledged(nameof(SetPiccOperatingParameterAsync), command, response);
        return (PiccOperatingParameters)response.Sw2;
    }

    /// <summary>§6.6 Sets the contactless-chip response timeout.</summary>
    public Task SetCardDetectionTimeoutAsync(CardDetectionTimeout timeout, CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(SetCardDetectionTimeoutAsync), Acr122uCommands.SetTimeout(timeout.ToByte()), cancellationToken);

    /// <summary>§6.7 Enables or disables the buzzer sounding automatically when a card is detected (default: on).</summary>
    public Task SetBuzzerOnCardDetectionAsync(bool enabled, CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(SetBuzzerOnCardDetectionAsync), Acr122uCommands.SetBuzzerOnCardDetection(enabled), cancellationToken);

    // ---------------------------------------------------------------------------------------------
    // §7.0 Contactless interface helpers
    // ---------------------------------------------------------------------------------------------

    /// <summary>Turns the antenna RF field on or off (§7.0, note 1) — e.g. to save power or force a re-read of the same tag.</summary>
    public Task SetAntennaAsync(bool on, CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(SetAntennaAsync), Acr122uCommands.SetAntenna(on), cancellationToken);

    /// <summary>§7.5 Returns the current contactless interface status (field presence, target bit rates, modulation).</summary>
    public async Task<ContactlessInterfaceStatus> GetContactlessInterfaceStatusAsync(CancellationToken cancellationToken = default)
    {
        var command = Acr122uCommands.GetContactlessInterfaceStatus();
        var response = await ExecuteAsync(nameof(GetContactlessInterfaceStatusAsync), command, cancellationToken).ConfigureAwait(false);
        return ParseContactlessInterfaceStatus(response.Data.Span);
    }

    private static ContactlessInterfaceStatus ParseContactlessInterfaceStatus(ReadOnlySpan<byte> raw)
    {
        // Expected framing (§7.5): D5 05h [Err] [Field] [NbTg] ([Tg] [BrRx] [BrTx] [Type] 80h)?
        if (raw.Length < 5 || raw[0] != 0xD5 || raw[1] != 0x05)
        {
            throw new Acr122uException("Unexpected response to the GetGeneralStatus (§7.5) command.");
        }

        var errorCode = (Acr122uErrorCode)raw[2];
        var fieldPresent = raw[3] != 0x00;
        var targetCount = raw[4];

        if (targetCount == 0 || raw.Length < 9)
        {
            return new ContactlessInterfaceStatus(errorCode, fieldPresent, targetCount, null, null, null, null);
        }

        return new ContactlessInterfaceStatus(
            errorCode,
            fieldPresent,
            targetCount,
            LogicalTargetNumber: raw[5],
            ReceiveBitRate: (ContactlessBitRate)raw[6],
            TransmitBitRate: (ContactlessBitRate)raw[7],
            ModulationType: (ContactlessModulationType)raw[8]);
    }

    // ---------------------------------------------------------------------------------------------
    // Connection lifecycle
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Waits until a card is present on the currently connected reader, (re)connecting the
    /// transport as needed. Mirrors the "Step 0" polling flow described in §7.0 of the specification.
    /// </summary>
    public async Task WaitForCardAsync(CancellationToken cancellationToken = default)
    {
        var readerName = ReaderName
            ?? throw new InvalidOperationException("The transport has not been connected to a reader yet.");

        if (_transport.IsConnected)
        {
            await _transport.DisconnectAsync(SmartCardDisposition.LeaveCard, cancellationToken).ConfigureAwait(false);
        }

        var loggedWaitMessage = false;
        await _transport.ConnectWhenCardPresentAsync(
            readerName,
            _options.CardPollInterval,
            _options.ShareMode,
            _options.CardWaitTimeout,
            onWaiting: _ =>
            {
                if (!loggedWaitMessage)
                {
                    loggedWaitMessage = true;
                    Log(Acr122uLogLevel.Information, $"Waiting for a card to be presented to '{readerName}'...");
                }
            },
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    // ---------------------------------------------------------------------------------------------
    // Internals
    // ---------------------------------------------------------------------------------------------

    private async Task<ResponseApdu> ExecuteAsync(string operationName, CommandApdu command, CancellationToken cancellationToken)
    {
        var response = await _transport.TransmitAsync(command, cancellationToken).ConfigureAwait(false);
        Log(Acr122uLogLevel.Trace, $"{operationName}: {command} -> {response}");
        ThrowIfNotAcknowledged(operationName, command, response);
        return response;
    }

    private static void ThrowIfNotAcknowledged(string operationName, CommandApdu command, ResponseApdu response)
    {
        // Most commands report success as SW1 SW2 == 90 00h. A handful of pseudo-APDU commands
        // (§6.2, §6.4, §6.5) instead report success as SW1 == 90h with an out-of-band value in
        // SW2 (e.g. the current LED state, or the current PICC operating parameter). Checking only
        // SW1 unifies both cases: every documented failure code (63 00h, 6A 81h) has SW1 != 90h.
        if (response.Sw1 != 0x90)
        {
            throw new Acr122uCommandException(operationName, command, response);
        }
    }

    private void Log(Acr122uLogLevel level, string message) => _options.Logger?.Invoke(new Acr122uLogEntry(level, message));

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_ownsTransport)
        {
            _transport.Dispose();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_ownsTransport)
        {
            await _transport.DisposeAsync().ConfigureAwait(false);
        }
    }
}

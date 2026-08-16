using Acs.Acr122u.Apdu;
using Acs.Acr122u.Models;

namespace Acs.Acr122u.Commands;

/// <summary>
/// Low-level, allocation-light builders for every command described in the ACR122U Application
/// Programming Interface specification (v2.04). These map directly to the byte sequences in the
/// document and perform no I/O; pair them with <see cref="Acr122uReader"/> (or your own
/// <see cref="Transport.ISmartCardTransport"/>) to actually talk to the reader.
/// </summary>
/// <remarks>
/// <see cref="Acr122uReader"/> already exposes a friendly, checked, async wrapper for every
/// command below — most applications should use that instead of calling these builders directly.
/// This class exists for advanced scenarios: building your own transport pipeline, logging/
/// replaying raw APDUs, or reaching a command this library's high-level API doesn't wrap yet.
/// </remarks>
public static class Acr122uCommands
{
    // ---- 4.0 PICC Commands for General Purposes ---------------------------------------------------

    /// <summary>§4.1 Get Data — retrieves the UID of the connected PICC.</summary>
    public static CommandApdu GetUid() => new(0xFF, 0xCA, 0x00, 0x00, le: 0x00);

    /// <summary>§4.1 Get Data — retrieves the ATS of an ISO 14443-4 Type A PICC.</summary>
    public static CommandApdu GetAts() => new(0xFF, 0xCA, 0x01, 0x00, le: 0x00);

    // ---- 5.0 PICC Commands (T=CL emulation) for MIFARE Classic memory cards -------------------------

    /// <summary>§5.1 Load Authentication Keys into the reader's volatile key store.</summary>
    public static CommandApdu LoadAuthenticationKey(KeySlot keySlot, ReadOnlyMemory<byte> key)
    {
        if (key.Length != 6)
        {
            throw new ArgumentException("A MIFARE authentication key is always 6 bytes long.", nameof(key));
        }

        return new CommandApdu(0xFF, 0x82, 0x00, (byte)keySlot, key);
    }

    /// <summary>§5.2 Authentication (PC/SC v2.07 format, 10 bytes) — authenticate a block using a previously loaded key.</summary>
    public static CommandApdu Authenticate(byte block, KeyType keyType, KeySlot keySlot)
    {
        byte[] data = [0x01, 0x00, block, (byte)keyType, (byte)keySlot];
        return new CommandApdu(0xFF, 0x86, 0x00, 0x00, data);
    }

    /// <summary>§5.2 Authentication (obsolete PC/SC v2.01 format, 6 bytes), kept for legacy reader/driver compatibility.</summary>
    public static CommandApdu AuthenticateLegacy(byte block, KeyType keyType, KeySlot keySlot) =>
        CommandApdu.FromRawBytes(0xFF, 0x88, 0x00, block, (byte)keyType, (byte)keySlot);

    /// <summary>§5.3 Read Binary Blocks — read up to 16 bytes from a block that has already been authenticated.</summary>
    public static CommandApdu ReadBinaryBlock(byte block, byte length)
    {
        if (length > 16)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "A maximum of 16 bytes can be read in a single call.");
        }

        return new CommandApdu(0xFF, 0xB0, 0x00, block, le: length);
    }

    /// <summary>§5.4 Update Binary Blocks — write 4 (MIFARE Ultralight) or 16 (MIFARE Classic) bytes to a block.</summary>
    public static CommandApdu UpdateBinaryBlock(byte block, ReadOnlyMemory<byte> data)
    {
        if (data.Length is not (4 or 16))
        {
            throw new ArgumentException(
                "Block data must be exactly 4 bytes (MIFARE Ultralight) or 16 bytes (MIFARE Classic 1K/4K).", nameof(data));
        }

        return new CommandApdu(0xFF, 0xD6, 0x00, block, data);
    }

    // ---- 5.5 Value Block Related Commands ------------------------------------------------------------

    /// <summary>§5.5.1 Value Block Operation — store, increment or decrement a value block.</summary>
    public static CommandApdu WriteValueBlock(byte block, ValueBlockOperation operation, int value)
    {
        Span<byte> data = stackalloc byte[5];
        data[0] = (byte)operation;
        BinaryPrimitives.WriteInt32BigEndian(data[1..], value);
        return new CommandApdu(0xFF, 0xD7, 0x00, block, data.ToArray());
    }

    /// <summary>§5.5.2 Read Value Block — read the signed 32-bit value stored in a value block.</summary>
    public static CommandApdu ReadValueBlock(byte block) => new(0xFF, 0xB1, 0x00, block, le: 0x04);

    /// <summary>§5.5.3 Restore Value Block — copy a value from one value block to another block in the same sector.</summary>
    public static CommandApdu RestoreValueBlock(byte sourceBlock, byte targetBlock) =>
        new(0xFF, 0xD7, 0x00, sourceBlock, new byte[] { 0x03, targetBlock });

    // ---- 6.0 Pseudo-APDU Commands --------------------------------------------------------------------

    /// <summary>§6.1 Direct Transmit — send a raw payload straight to the tag/reader.</summary>
    public static CommandApdu DirectTransmit(ReadOnlyMemory<byte> payload)
    {
        if (payload.Length > CommandApdu.MaxDataLength)
        {
            throw new ArgumentOutOfRangeException(nameof(payload), payload.Length, $"Maximum payload is {CommandApdu.MaxDataLength} bytes.");
        }

        return new CommandApdu(0xFF, 0x00, 0x00, 0x00, payload);
    }

    /// <summary>§6.2 Bi-color LED and Buzzer Control.</summary>
    public static CommandApdu SetLedAndBuzzer(LedBuzzerControlRequest request) =>
        new(0xFF, 0x00, 0x40, request.StateControlByte, request.ToBytes());

    /// <summary>§6.3 Get firmware version of the reader.</summary>
    public static CommandApdu GetFirmwareVersion() => new(0xFF, 0x00, 0x48, 0x00, le: 0x00);

    /// <summary>§6.4 Get the PICC operating parameter.</summary>
    public static CommandApdu GetPiccOperatingParameter() => new(0xFF, 0x00, 0x50, 0x00, le: 0x00);

    /// <summary>§6.5 Set the PICC operating parameter.</summary>
    public static CommandApdu SetPiccOperatingParameter(PiccOperatingParameters parameters) =>
        new(0xFF, 0x00, 0x51, (byte)parameters, le: 0x00);

    /// <summary>§6.6 Set Timeout Parameter — sets the contactless chip response timeout.</summary>
    public static CommandApdu SetTimeout(byte fiveSecondUnits) => new(0xFF, 0x00, 0x41, fiveSecondUnits, le: 0x00);

    /// <summary>§6.7 Set buzzer output during card detection (default: on).</summary>
    public static CommandApdu SetBuzzerOnCardDetection(bool enabled) =>
        new(0xFF, 0x00, 0x52, (byte)(enabled ? 0xFF : 0x00), le: 0x00);

    // ---- 7.0 Contactless interface helpers (PN532 escape frames, §7.0/§7.5) -------------------------

    /// <summary>Turns the antenna RF field on or off (§7.0, note 1) — e.g. to save power or force a re-read of the same tag.</summary>
    public static CommandApdu SetAntenna(bool on) =>
        DirectTransmit(new byte[] { 0xD4, 0x32, 0x01, (byte)(on ? 0x01 : 0x00) });

    /// <summary>§7.5 Get the current setting of the contactless interface (PN532 GetGeneralStatus frame).</summary>
    public static CommandApdu GetContactlessInterfaceStatus() => DirectTransmit(new byte[] { 0xD4, 0x04 });
}

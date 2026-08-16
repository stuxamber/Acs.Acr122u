namespace Acs.Acr122u.Apdu;

/// <summary>
/// Represents an ISO/IEC 7816-4 style command APDU as consumed by the ACR122U, including the
/// reader's "pseudo-APDU" commands described in the ACR122U API specification (v2.04), §6.0.
/// </summary>
/// <remarks>
/// Only the short APDU forms ("case 1" through "case 4") used throughout the ACR122U command set
/// are supported; extended-length APDUs are not part of this reader's command set.
/// This type is an immutable, allocation-light value type designed to be built once and reused.
/// </remarks>
public readonly struct CommandApdu : IEquatable<CommandApdu>
{
    /// <summary>Maximum number of bytes that can be carried in <see cref="Data"/> for a short APDU.</summary>
    public const int MaxDataLength = 255;

    private readonly byte[]? _rawOverride;

    /// <summary>The instruction class byte (always FFh for ACR122U commands).</summary>
    public byte Class { get; }

    /// <summary>The instruction code byte.</summary>
    public byte Instruction { get; }

    /// <summary>Parameter byte 1.</summary>
    public byte P1 { get; }

    /// <summary>Parameter byte 2.</summary>
    public byte P2 { get; }

    /// <summary>The command data field (sent as Lc + data), if any.</summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>The expected response length (Le), if any.</summary>
    public byte? Le { get; }

    /// <summary>
    /// Builds a standard APDU, automatically inserting the Lc length byte whenever
    /// <paramref name="data"/> is non-empty, and the Le byte whenever <paramref name="le"/> is
    /// supplied — matching every "Command Format" table in the ACR122U specification.
    /// </summary>
    public CommandApdu(byte cla, byte ins, byte p1, byte p2, ReadOnlyMemory<byte> data = default, byte? le = null)
    {
        if (data.Length > MaxDataLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(data), data.Length, $"APDU data length cannot exceed {MaxDataLength} bytes.");
        }

        Class = cla;
        Instruction = ins;
        P1 = p1;
        P2 = p2;
        Data = data;
        Le = le;
        _rawOverride = null;
    }

    private CommandApdu(byte[] rawBytes)
    {
        if (rawBytes.Length < 4)
        {
            throw new ArgumentException(
                "An APDU must contain at least the CLA, INS, P1 and P2 bytes.", nameof(rawBytes));
        }

        Class = rawBytes[0];
        Instruction = rawBytes[1];
        P1 = rawBytes[2];
        P2 = rawBytes[3];
        Data = rawBytes.Length > 4 ? rawBytes[4..] : Array.Empty<byte>();
        Le = null;
        _rawOverride = rawBytes;
    }

    /// <summary>
    /// Builds an APDU from its exact wire bytes, bypassing the automatic Lc-insertion logic. Use
    /// this for the handful of non-standard, fixed-layout commands in the specification (e.g. the
    /// obsolete §5.2 authentication format), or to send any command this library doesn't model yet.
    /// </summary>
    public static CommandApdu FromRawBytes(params byte[] apdu)
    {
        ArgumentNullException.ThrowIfNull(apdu);
        return new CommandApdu(apdu);
    }

    /// <summary>The total length, in bytes, of the wire representation of this APDU.</summary>
    public int Length => _rawOverride?.Length
        ?? 4 + (Data.Length > 0 ? 1 + Data.Length : 0) + (Le.HasValue ? 1 : 0);

    /// <summary>Returns the wire representation of this APDU as a newly allocated array.</summary>
    public byte[] ToByteArray()
    {
        var buffer = new byte[Length];
        WriteTo(buffer);
        return buffer;
    }

    /// <summary>Writes the wire representation of this APDU into <paramref name="destination"/> without allocating.</summary>
    /// <returns>The number of bytes written.</returns>
    public int WriteTo(Span<byte> destination)
    {
        var required = Length;
        if (destination.Length < required)
        {
            throw new ArgumentException("Destination buffer is too small.", nameof(destination));
        }

        if (_rawOverride is { } raw)
        {
            raw.CopyTo(destination);
            return raw.Length;
        }

        var offset = 0;
        destination[offset++] = Class;
        destination[offset++] = Instruction;
        destination[offset++] = P1;
        destination[offset++] = P2;

        if (Data.Length > 0)
        {
            destination[offset++] = (byte)Data.Length;
            Data.Span.CopyTo(destination[offset..]);
            offset += Data.Length;
        }

        if (Le.HasValue)
        {
            destination[offset++] = Le.Value;
        }

        return offset;
    }

    /// <inheritdoc />
    public override string ToString() => Convert.ToHexString(ToByteArray());

    /// <inheritdoc />
    public bool Equals(CommandApdu other) => ToByteArray().AsSpan().SequenceEqual(other.ToByteArray());

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is CommandApdu other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var b in ToByteArray())
        {
            hash.Add(b);
        }

        return hash.ToHashCode();
    }

    /// <summary>Returns whether two APDUs have the same wire representation.</summary>
    public static bool operator ==(CommandApdu left, CommandApdu right) => left.Equals(right);

    /// <summary>Returns whether two APDUs have different wire representations.</summary>
    public static bool operator !=(CommandApdu left, CommandApdu right) => !left.Equals(right);
}

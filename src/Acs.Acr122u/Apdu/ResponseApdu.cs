using Acs.Acr122u.Diagnostics;

namespace Acs.Acr122u.Apdu;

/// <summary>
/// The response returned by the reader to a <see cref="CommandApdu"/>: zero or more data bytes
/// followed by the two status bytes SW1 and SW2, as described throughout the ACR122U API
/// specification's "Response Format" / "Response Codes" tables.
/// </summary>
public readonly struct ResponseApdu
{
    /// <summary>The response data field, excluding the trailing SW1/SW2 status bytes.</summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>The first status byte.</summary>
    public byte Sw1 { get; }

    /// <summary>The second status byte.</summary>
    public byte Sw2 { get; }

    /// <summary>Constructs a response APDU from its already-separated data and status bytes.</summary>
    public ResponseApdu(ReadOnlyMemory<byte> data, byte sw1, byte sw2)
    {
        Data = data;
        Sw1 = sw1;
        Sw2 = sw2;
    }

    /// <summary>The two status bytes combined into a single value for convenient comparison.</summary>
    public StatusWord Status => new(Sw1, Sw2);

    /// <summary>
    /// True when the status word is 90 00h, which the ACR122U uses to indicate a successfully
    /// completed operation for the vast majority of commands in the specification. A small number
    /// of commands (§6.2, §6.4, §6.5) instead report success as SW1 = 90h with an out-of-band value
    /// in SW2 — see the remarks on the corresponding <see cref="Acr122uReader"/> methods.
    /// </summary>
    public bool IsSuccess => Sw1 == 0x90 && Sw2 == 0x00;

    /// <summary>Parses a raw byte sequence returned by the transport into a <see cref="ResponseApdu"/>.</summary>
    public static ResponseApdu Parse(ReadOnlySpan<byte> raw)
    {
        if (raw.Length < 2)
        {
            throw new ArgumentException(
                "A response APDU must contain at least the SW1 and SW2 status bytes.", nameof(raw));
        }

        var data = raw[..^2].ToArray();
        return new ResponseApdu(data, raw[^2], raw[^1]);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Convert.ToHexString(Data.Span)} SW={Sw1:X2}{Sw2:X2}h";
}

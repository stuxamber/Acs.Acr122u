namespace Acs.Acr122u.Diagnostics;

/// <summary>
/// A two-byte ISO/IEC 7816-4 style status word (SW1 SW2), as returned by the ACR122U for every
/// APDU command it processes. See the "Response Codes" tables throughout the ACR122U API
/// specification (v2.04).
/// </summary>
public readonly record struct StatusWord(byte Sw1, byte Sw2)
{
    /// <summary>90 00h — the operation completed successfully.</summary>
    public static readonly StatusWord Success = new(0x90, 0x00);

    /// <summary>63 00h — the operation failed.</summary>
    public static readonly StatusWord OperationFailed = new(0x63, 0x00);

    /// <summary>6A 81h — the requested function is not supported.</summary>
    public static readonly StatusWord FunctionNotSupported = new(0x6A, 0x81);

    /// <summary>The status word as a single 16-bit value (SW1 in the high byte).</summary>
    public ushort Value => (ushort)((Sw1 << 8) | Sw2);

    /// <summary>True when this status word equals <see cref="Success"/>.</summary>
    public bool IsSuccess => this == Success;

    /// <inheritdoc />
    public override string ToString() => $"{Sw1:X2}{Sw2:X2}h";

    /// <summary>Widens this status word to its combined 16-bit value (equivalent to <see cref="Value"/>).</summary>
    public static implicit operator ushort(StatusWord sw) => sw.Value;

    /// <summary>Named alternative to the implicit <see cref="ushort"/> conversion above.</summary>
    public ushort ToUInt16() => Value;
}

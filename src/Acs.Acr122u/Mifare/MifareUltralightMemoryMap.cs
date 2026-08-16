namespace Acs.Acr122u.Mifare;

/// <summary>Page layout constants for MIFARE Ultralight cards (§5.2, Table 6).</summary>
public static class MifareUltralightMemoryMap
{
    /// <summary>Page 0 — the first 4 bytes of the 7-byte UID/serial number.</summary>
    public const int SerialNumberPage0 = 0;

    /// <summary>Page 1 — the remaining bytes of the UID/serial number plus internal/check bytes.</summary>
    public const int SerialNumberPage1 = 1;

    /// <summary>Page 2 — internal bytes and the static lock bits.</summary>
    public const int InternalLockPage = 2;

    /// <summary>Page 3 — the one-time-programmable (OTP) bytes.</summary>
    public const int OneTimeProgrammablePage = 3;

    /// <summary>The first page of user-writable data memory.</summary>
    public const int FirstUserDataPage = 4;

    /// <summary>The last page of user-writable data memory.</summary>
    public const int LastUserDataPage = 15;

    /// <summary>Number of bytes stored per page.</summary>
    public const int PageSizeInBytes = 4;

    /// <summary>Total addressable memory capacity, in bytes.</summary>
    public const int TotalCapacityInBytes = 64;
}

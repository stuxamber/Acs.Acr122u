using Acs.Acr122u.Diagnostics;

namespace Acs.Acr122u.Models;

/// <summary>Bit rate used on the contactless interface (§7.5).</summary>
public enum ContactlessBitRate : byte
{
    /// <summary>106 kbps.</summary>
    Kbps106 = 0x00,

    /// <summary>212 kbps.</summary>
    Kbps212 = 0x01,

    /// <summary>424 kbps.</summary>
    Kbps424 = 0x02,
}

/// <summary>Modulation type of the currently detected target (§7.5).</summary>
public enum ContactlessModulationType : byte
{
    /// <summary>ISO 14443 Type A / MIFARE modulation.</summary>
    Iso14443OrMifare = 0x00,

    /// <summary>ISO 18092 active-mode modulation.</summary>
    ActiveMode = 0x01,

    /// <summary>Innovision Jewel (NFC Forum Type 1) modulation.</summary>
    InnovisionJewel = 0x02,

    /// <summary>FeliCa modulation.</summary>
    FeliCa = 0x10,
}

/// <summary>
/// A snapshot of the contactless interface as reported by the PN532 "GetGeneralStatus" frame
/// (§7.5). Retrieve it with <see cref="Acr122uReader.GetContactlessInterfaceStatusAsync"/>.
/// </summary>
public sealed record ContactlessInterfaceStatus(
    Acr122uErrorCode ErrorCode,
    bool ExternalFieldPresent,
    byte TargetCount,
    byte? LogicalTargetNumber,
    ContactlessBitRate? ReceiveBitRate,
    ContactlessBitRate? TransmitBitRate,
    ContactlessModulationType? ModulationType);

namespace Acs.Acr122u.Models;

/// <summary>
/// PICC operating parameter bit flags (§6.4 / §6.5). The default value on power-up is
/// <see cref="All"/> (FFh) — every tag type is polled, at a 500&#160;ms interval, with automatic
/// ATS generation enabled.
/// </summary>
[Flags]
public enum PiccOperatingParameters : byte
{
    /// <summary>No polling/parameter bits set.</summary>
    None = 0,

    /// <summary>Detect ISO 14443 Type A tags during polling (bit 0). Disable automatic ATS generation to detect MIFARE tags.</summary>
    DetectIso14443TypeA = 1 << 0,

    /// <summary>Detect ISO 14443 Type B tags during polling (bit 1).</summary>
    DetectIso14443TypeB = 1 << 1,

    /// <summary>Detect Topaz/Jewel tags during polling (bit 2).</summary>
    DetectTopaz = 1 << 2,

    /// <summary>Detect FeliCa 212K tags during polling (bit 3).</summary>
    DetectFeliCa212K = 1 << 3,

    /// <summary>Detect FeliCa 424K tags during polling (bit 4).</summary>
    DetectFeliCa424K = 1 << 4,

    /// <summary>Poll every 250&#160;ms instead of the default 500&#160;ms (bit 5).</summary>
    PollingInterval250Ms = 1 << 5,

    /// <summary>Automatically issue an ATS request whenever an ISO 14443-4 Type A tag is activated (bit 6).</summary>
    AutoAtsGeneration = 1 << 6,

    /// <summary>Enable automatic, continuous PICC polling (bit 7).</summary>
    AutoPiccPolling = 1 << 7,

    /// <summary>The factory-default parameter value (FFh): every flag enabled.</summary>
    All = DetectIso14443TypeA | DetectIso14443TypeB | DetectTopaz | DetectFeliCa212K | DetectFeliCa424K
        | PollingInterval250Ms | AutoAtsGeneration | AutoPiccPolling,
}

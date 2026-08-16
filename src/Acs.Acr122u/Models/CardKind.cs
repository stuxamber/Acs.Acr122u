namespace Acs.Acr122u.Models;

/// <summary>Well-known PICC types identifiable from the ATR "card name" bytes (§3.1.1) or ATS presence (§3.1.2).</summary>
public enum CardKind
{
    /// <summary>The ATR could not be recognized against any pattern in §3.1.</summary>
    Unknown,

    /// <summary>MIFARE Classic 1K.</summary>
    MifareClassic1K,

    /// <summary>MIFARE Classic 4K.</summary>
    MifareClassic4K,

    /// <summary>MIFARE Ultralight.</summary>
    MifareUltralight,

    /// <summary>MIFARE Mini.</summary>
    MifareMini,

    /// <summary>An NFC Forum Type 1 (Topaz/Jewel) tag.</summary>
    TopazOrJewel,

    /// <summary>A FeliCa tag operating at 212 kbps.</summary>
    FeliCa212K,

    /// <summary>A FeliCa tag operating at 424 kbps.</summary>
    FeliCa424K,

    /// <summary>An ISO 14443-4 tag (e.g. MIFARE DESFire, a JavaCard applet, ...) identified by its ATS/ATTRIB response rather than a §3.1.1 card-name pair.</summary>
    Iso14443Part4,
}

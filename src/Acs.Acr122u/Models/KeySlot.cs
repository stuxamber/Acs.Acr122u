namespace Acs.Acr122u.Models;

/// <summary>
/// A volatile key-storage slot in the reader (§5.1). The ACR122U exposes two slots; keys loaded
/// into a slot are lost when the reader is disconnected from the host.
/// </summary>
public enum KeySlot : byte
{
    /// <summary>The first key slot.</summary>
    Slot0 = 0x00,

    /// <summary>The second key slot.</summary>
    Slot1 = 0x01,
}

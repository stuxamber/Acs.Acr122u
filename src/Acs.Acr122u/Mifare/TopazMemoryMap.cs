namespace Acs.Acr122u.Mifare;

/// <summary>
/// Memory addressing helper for NFC Forum Type 1 (Topaz/Jewel) tags (§7.4, Figure 4).
/// Memory Address = Block * 8 + Byte, and each block/page is 8 bytes.
/// </summary>
public static class TopazMemoryMap
{
    /// <summary>Number of bytes stored per block/page.</summary>
    public const int BlockSizeInBytes = 8;

    /// <summary>Block 0 — the tag's UID and manufacturer/lock bytes.</summary>
    public const int UidBlock = 0;

    /// <summary>The first block of user-writable data memory.</summary>
    public const int FirstDataBlock = 1;

    /// <summary>The last block of user-writable data memory.</summary>
    public const int LastDataBlock = 0x0C;

    /// <summary>The block holding the lock and reserved bytes.</summary>
    public const int LockReservedBlock = 0x0E;

    /// <summary>Converts a (block, byte-offset) pair into the flat memory address used by §7.4 read/write commands.</summary>
    public static byte GetMemoryAddress(byte block, byte byteOffset)
    {
        if (byteOffset > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(byteOffset), byteOffset, "Byte offset must be 0-7.");
        }

        return (byte)((block * 8) + byteOffset);
    }
}

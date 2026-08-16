namespace Acs.Acr122u.Mifare;

/// <summary>
/// Sector/block layout helpers for MIFARE Classic 1K and 4K memory cards (§5.2, Tables 4 &amp; 5).
/// All methods work uniformly across both capacities: 1K cards simply never use sector numbers
/// 32 and above.
/// </summary>
public static class MifareClassicMemoryMap
{
    /// <summary>Number of sectors on a MIFARE Classic 1K card, each holding 4 blocks (Table 4).</summary>
    public const int Sectors1K = 16;

    /// <summary>Number of 4-block ("small") sectors at the start of a MIFARE Classic 4K card (Table 5).</summary>
    public const int SmallSectors4K = 32;

    /// <summary>Number of 16-block ("large") sectors at the end of a MIFARE Classic 4K card (Table 5).</summary>
    public const int LargeSectors4K = 8;

    /// <summary>Number of bytes per block.</summary>
    public const int BlockSizeInBytes = 16;

    /// <summary>Returns the trailer (keys + access-bits) block number for the given sector.</summary>
    public static byte GetTrailerBlock(int sector)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sector);
        return sector < SmallSectors4K
            ? (byte)((sector * 4) + 3)
            : (byte)((SmallSectors4K * 4) + ((sector - SmallSectors4K) * 16) + 15);
    }

    /// <summary>Returns the first data block number of the given sector.</summary>
    public static byte GetFirstDataBlock(int sector)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sector);
        return sector < SmallSectors4K
            ? (byte)(sector * 4)
            : (byte)((SmallSectors4K * 4) + ((sector - SmallSectors4K) * 16));
    }

    /// <summary>Returns the number of usable data blocks (i.e. excluding the trailer) in the given sector.</summary>
    public static int GetDataBlockCount(int sector)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sector);
        return sector < SmallSectors4K ? 3 : 15;
    }

    /// <summary>Returns the zero-based sector number that the given block belongs to.</summary>
    public static int GetSectorForBlock(byte block)
    {
        const int smallSectorBlocks = SmallSectors4K * 4; // 128
        return block < smallSectorBlocks
            ? block / 4
            : SmallSectors4K + ((block - smallSectorBlocks) / 16);
    }

    /// <summary>True when <paramref name="block"/> is a sector trailer block (holds the keys and access bits).</summary>
    public static bool IsTrailerBlock(byte block) => block == GetTrailerBlock(GetSectorForBlock(block));
}

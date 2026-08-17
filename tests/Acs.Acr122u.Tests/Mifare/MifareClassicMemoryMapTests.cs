using Acs.Acr122u.Mifare;
using Xunit;

namespace Acs.Acr122u.Tests.Mifare;

/// <summary>
/// Verifies the sector/block arithmetic in <see cref="MifareClassicMemoryMap"/> against the
/// layouts documented in spec Table 4 (MIFARE Classic 1K) and Table 5 (MIFARE Classic 4K).
/// </summary>
public sealed class MifareClassicMemoryMapTests
{
    [Theory]
    [InlineData(0, 3)] // Table 4: sector 0 -> blocks 0-3, trailer = block 3
    [InlineData(1, 7)] // sector 1 -> blocks 4-7, trailer = block 7
    [InlineData(15, 63)] // sector 15 (last "small" sector on a 1K card) -> trailer = block 63 (0x3Fh)
    [InlineData(31, 127)] // sector 31 (last "small" sector on a 4K card) -> trailer = block 127 (0x7Fh)
    [InlineData(32, 143)] // Table 5: sector 32 (first "large", 16-block sector) -> trailer = block 143 (0x8Fh)
    [InlineData(39, 255)] // sector 39 (last "large" sector on a 4K card) -> trailer = block 255 (0xFFh)
    public void GetTrailerBlockMatchesSpecLayout(int sector, byte expectedTrailerBlock)
    {
        Assert.Equal(expectedTrailerBlock, MifareClassicMemoryMap.GetTrailerBlock(sector));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 4)]
    [InlineData(15, 60)]
    [InlineData(31, 124)]
    [InlineData(32, 128)]
    [InlineData(39, 240)]
    public void GetFirstDataBlockMatchesSpecLayout(int sector, byte expectedFirstBlock)
    {
        Assert.Equal(expectedFirstBlock, MifareClassicMemoryMap.GetFirstDataBlock(sector));
    }

    [Theory]
    [InlineData(0, 3)] // "small" sectors: 4 blocks total, 3 usable as data
    [InlineData(31, 3)]
    [InlineData(32, 15)] // "large" sectors: 16 blocks total, 15 usable as data
    [InlineData(39, 15)]
    public void GetDataBlockCountMatchesSpecLayout(int sector, int expectedDataBlockCount)
    {
        Assert.Equal(expectedDataBlockCount, MifareClassicMemoryMap.GetDataBlockCount(sector));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 1)]
    [InlineData(63, 15)]
    [InlineData(127, 31)]
    [InlineData(128, 32)] // first block of the first "large" sector
    [InlineData(143, 32)]
    [InlineData(144, 33)]
    [InlineData(255, 39)]
    public void GetSectorForBlockIsTheInverseOfGetFirstDataBlockAndGetTrailerBlock(byte block, int expectedSector)
    {
        Assert.Equal(expectedSector, MifareClassicMemoryMap.GetSectorForBlock(block));
    }

    [Theory]
    [InlineData((byte)3, true)]
    [InlineData((byte)0, false)]
    [InlineData((byte)143, true)]
    [InlineData((byte)128, false)]
    public void IsTrailerBlockMatchesGetTrailerBlock(byte block, bool expected)
    {
        Assert.Equal(expected, MifareClassicMemoryMap.IsTrailerBlock(block));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(39)]
    public void EveryBlockInASectorRoundTripsThroughGetSectorForBlock(int sector)
    {
        // For every block belonging to a sector, GetSectorForBlock must map it back to that
        // sector — including the trailer block and every data block, for both small and large
        // sector shapes. Loop counter is deliberately int, not byte: sector 39's trailer block is
        // 255 (byte.MaxValue), and a byte-typed loop counter would wrap back to 0 and spin forever.
        var firstBlock = MifareClassicMemoryMap.GetFirstDataBlock(sector);
        var trailerBlock = MifareClassicMemoryMap.GetTrailerBlock(sector);

        for (var blockNumber = (int)firstBlock; blockNumber <= trailerBlock; blockNumber++)
        {
            Assert.Equal(sector, MifareClassicMemoryMap.GetSectorForBlock((byte)blockNumber));
        }

        Assert.True(MifareClassicMemoryMap.IsTrailerBlock(trailerBlock));
        Assert.Equal(MifareClassicMemoryMap.GetDataBlockCount(sector), trailerBlock - firstBlock);
    }
}

using Acs.Acr122u.Models;
using Xunit;

namespace Acs.Acr122u.Tests.Models;

/// <summary>
/// Verifies <see cref="AtrInfo.Parse"/> against every worked ATR example in the specification
/// (§3.1.1's Table 2 example and §3.1.2's two ISO 14443-4 examples).
/// </summary>
public sealed class AtrInfoTests
{
    [Fact]
    public void ParseMifare1KExampleRecognizesCardKind()
    {
        // §3.1.1, Table 2 worked example:
        // ATR for MIFARE 1K = {3B 8F 80 01 80 4F 0C A0 00 00 03 06 03 00 01 00 00 00 00 6Ah}
        byte[] atr = Convert.FromHexString("3B8F8001804F0CA000000306030001000000006A");

        var info = AtrInfo.Parse(atr);

        Assert.Equal(CardKind.MifareClassic1K, info.Kind);
        Assert.Null(info.HistoricalBytes);
        Assert.Equal(atr, info.Raw);
    }

    [Fact]
    public void ParseDESFireExampleExtractsFullAtsAsHistoricalBytes()
    {
        // §3.1.2 example: "DESFire (ATR) = 3B 86 80 01 06 75 77 81 02 80 00h"
        // "This ATR has 6 bytes of ATS, which is: [06 75 77 81 02 80h]"
        byte[] atr = Convert.FromHexString("3B86800106757781028000");

        var info = AtrInfo.Parse(atr);

        Assert.Equal(CardKind.Iso14443Part4, info.Kind);
        Assert.Equal(Convert.FromHexString("067577810280"), info.HistoricalBytes);
    }

    [Fact]
    public void ParseST19XRC8EExampleExtractsFullAtqbAsHistoricalBytes()
    {
        // §3.1.2 example: "ST19XRC8E (ATR) = 3B 8C 80 01 50 12 23 45 56 12 53 54 4E 33 81 C3 55h"
        // "the response would be ATQB which is 50 12 23 45 56 12 53 54 4E 33 81 C3h is 12 bytes long"
        byte[] atr = Convert.FromHexString("3B8C800150122345561253544E3381C355");

        var info = AtrInfo.Parse(atr);

        Assert.Equal(CardKind.Iso14443Part4, info.Kind);
        Assert.Equal(Convert.FromHexString("50122345561253544E3381C3"), info.HistoricalBytes);
    }

    [Theory]
    [InlineData("0001", CardKind.MifareClassic1K)]
    [InlineData("0002", CardKind.MifareClassic4K)]
    [InlineData("0003", CardKind.MifareUltralight)]
    [InlineData("0026", CardKind.MifareMini)]
    [InlineData("F004", CardKind.TopazOrJewel)]
    [InlineData("F011", CardKind.FeliCa212K)]
    [InlineData("F012", CardKind.FeliCa424K)]
    [InlineData("FFFF", CardKind.Unknown)]
    public void ParseRecognizesEveryDocumentedCardNamePair(string cardNameHex, CardKind expectedKind)
    {
        // Same MIFARE 1K ATR skeleton from §3.1.1, with the card-name bytes (indices 13-14) swapped
        // for each documented pair in the §3.1.1 card-name table.
        var atr = Convert.FromHexString("3B8F8001804F0CA000000306030001000000006A");
        var cardName = Convert.FromHexString(cardNameHex);
        atr[13] = cardName[0];
        atr[14] = cardName[1];

        var info = AtrInfo.Parse(atr);

        Assert.Equal(expectedKind, info.Kind);
    }

    [Fact]
    public void ParseTooShortOrWrongInitialHeaderReturnsUnknownRatherThanThrowing()
    {
        Assert.Equal(CardKind.Unknown, AtrInfo.Parse(Convert.FromHexString("3B00")).Kind);
        Assert.Equal(CardKind.Unknown, AtrInfo.Parse(Convert.FromHexString("0011223344")).Kind);
    }
}

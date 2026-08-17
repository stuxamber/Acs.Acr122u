using Acs.Acr122u.Apdu;
using Xunit;

namespace Acs.Acr122u.Tests.Apdu;

public sealed class ResponseApduTests
{
    [Fact]
    public void ParseSplitsTrailingTwoBytesAsStatusWord()
    {
        // A typical §5.3 Read Binary Block success response: 4 data bytes + SW1 SW2 = 90 00h.
        byte[] raw = [0xDE, 0xAD, 0xBE, 0xEF, 0x90, 0x00];

        var response = ResponseApdu.Parse(raw);

        byte[] expectedData = [0xDE, 0xAD, 0xBE, 0xEF];
        Assert.Equal(expectedData, response.Data.ToArray());
        Assert.Equal(0x90, response.Sw1);
        Assert.Equal(0x00, response.Sw2);
        Assert.True(response.IsSuccess);
    }

    [Fact]
    public void ParseMinimalTwoByteResponseHasEmptyData()
    {
        byte[] raw = [0x63, 0x00];

        var response = ResponseApdu.Parse(raw);

        Assert.Empty(response.Data.ToArray());
        Assert.Equal(0x63, response.Sw1);
        Assert.Equal(0x00, response.Sw2);
        Assert.False(response.IsSuccess);
    }

    [Fact]
    public void ParseFewerThanTwoBytesThrows()
    {
        byte[] raw = [0x90];

        Assert.Throws<ArgumentException>(() => ResponseApdu.Parse(raw));
    }

    [Theory]
    [InlineData(0x90, 0x00, true)]
    [InlineData(0x63, 0x00, false)] // §5.x "operation failed" documented error code
    [InlineData(0x6A, 0x81, false)] // documented "function not supported" error code
    [InlineData(0x90, 0x03, false)] // SW1=90h but SW2 != 00h: IsSuccess is strict 90 00h only
    public void IsSuccessOnlyTrueForExactly9000h(byte sw1, byte sw2, bool expected)
    {
        var response = new ResponseApdu(Array.Empty<byte>(), sw1, sw2);
        Assert.Equal(expected, response.IsSuccess);
    }
}

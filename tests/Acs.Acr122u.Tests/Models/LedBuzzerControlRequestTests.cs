using Acs.Acr122u.Commands;
using Acs.Acr122u.Models;
using Xunit;

namespace Acs.Acr122u.Tests.Models;

public sealed class LedBuzzerControlRequestTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(500, 5)] // Appendix E examples 5-7: 500 ms -> 05h
    [InlineData(2000, 20)] // Appendix E example 4: 2000 ms -> 14h
    [InlineData(25500, 255)] // maximum representable duration
    public void BlinkRoundsDurationToNearest100MsUnit(int milliseconds, byte expectedUnitByte)
    {
        var request = LedBuzzerControlRequest.Blink(
            red: true, green: false,
            onDuration: TimeSpan.FromMilliseconds(milliseconds), offDuration: TimeSpan.Zero,
            repeatCount: 1);

        var data = Acr122uCommands.SetLedAndBuzzer(request).ToByteArray();

        // Wire layout: FF 00 40 <P2> 04 <T1> <T2> <repeat> <buzzerLink> -- T1 is byte index 5.
        Assert.Equal(expectedUnitByte, data[5]);
    }

    [Theory]
    [InlineData(549, 5)] // rounds down (5.49 units)
    [InlineData(551, 6)] // rounds up (5.51 units)
    [InlineData(550, 6)] // exact midpoint rounds away from zero, matching MidpointRounding.AwayFromZero
    public void BlinkRoundsFractionalUnitsAwayFromZero(int milliseconds, byte expectedUnitByte)
    {
        var request = LedBuzzerControlRequest.Blink(
            red: true, green: false,
            onDuration: TimeSpan.FromMilliseconds(milliseconds), offDuration: TimeSpan.Zero,
            repeatCount: 1);

        var data = Acr122uCommands.SetLedAndBuzzer(request).ToByteArray();

        Assert.Equal(expectedUnitByte, data[5]);
    }

    [Fact]
    public void BlinkDurationBeyond25500MsThrows()
    {
        var request = LedBuzzerControlRequest.Blink(
            red: true, green: false,
            onDuration: TimeSpan.FromMilliseconds(25600), offDuration: TimeSpan.Zero,
            repeatCount: 1);

        // The 100 ms-unit range check happens when the request is converted to wire bytes
        // (ToBytes(), called from SetLedAndBuzzer), not when Blink() builds the request itself.
        Assert.Throws<ArgumentOutOfRangeException>(() => Acr122uCommands.SetLedAndBuzzer(request));
    }

    [Fact]
    public void BlinkNegativeDurationThrows()
    {
        var request = LedBuzzerControlRequest.Blink(
            red: true, green: false,
            onDuration: TimeSpan.FromMilliseconds(-100), offDuration: TimeSpan.Zero,
            repeatCount: 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => Acr122uCommands.SetLedAndBuzzer(request));
    }
}

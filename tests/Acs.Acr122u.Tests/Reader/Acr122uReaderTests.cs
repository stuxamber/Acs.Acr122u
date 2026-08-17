using Acs.Acr122u;
using Acs.Acr122u.Exceptions;
using Acs.Acr122u.Models;
using Acs.Acr122u.Tests.Fakes;
using Xunit;

namespace Acs.Acr122u.Tests.Reader;

/// <summary>
/// Verifies <see cref="Acr122uReader"/>'s parsing and status-word validation logic against the
/// spec's documented response formats and worked examples, using a fake transport so no real
/// reader or Windows PC/SC service is required.
/// </summary>
public sealed class Acr122uReaderTests
{
    // ---- §6.3 Get Firmware Version ------------------------------------------------------------------

    [Fact]
    public async Task GetFirmwareVersionAsyncSpecWorkedExampleReturnsFullString()
    {
        // §6.3 worked example: Response = 41 43 52 31 32 32 55 32 30 31h = "ACR122U201" (ASCII).
        // The response format table for this command is documented as a bare "(10 bytes)" with no
        // SW1/SW2 trailer, unlike every other command's "[data] + 2 bytes" format.
        using var transport = new FakeSmartCardTransport();
        transport.EnqueueResponse(Convert.FromHexString("41435231323255323031"));
        using var reader = new Acr122uReader(transport);

        var version = await reader.GetFirmwareVersionAsync().ConfigureAwait(true);

        Assert.Equal("ACR122U201", version);
    }

    // ---- §4.1 Get Data --------------------------------------------------------------------------

    [Fact]
    public async Task GetUidAsyncReturnsDataBytesOnSuccess()
    {
        using var transport = new FakeSmartCardTransport();
        transport.EnqueueResponse(0x04, 0xA3, 0x22, 0x91, 0x90, 0x00);
        using var reader = new Acr122uReader(transport);

        var uid = await reader.GetUidAsync().ConfigureAwait(true);

        byte[] expectedUid = [0x04, 0xA3, 0x22, 0x91];
        Assert.Equal(expectedUid, uid);
    }

    [Fact]
    public async Task GetUidAsyncFailureStatusWordThrowsWithOperationNameAndStatusWord()
    {
        using var transport = new FakeSmartCardTransport();
        transport.EnqueueResponse(0x63, 0x00); // documented "operation failed" error code
        using var reader = new Acr122uReader(transport);

        var exception = await Assert.ThrowsAsync<Acr122uCommandException>(() => reader.GetUidAsync()).ConfigureAwait(true);

        Assert.Equal(nameof(reader.GetUidAsync), exception.OperationName);
        Assert.Equal(0x63, exception.Response.Sw1);
        Assert.Equal(0x00, exception.Response.Sw2);
    }

    // ---- §5.2 Authenticate ------------------------------------------------------------------------

    [Fact]
    public async Task TryAuthenticateAsyncReturnsTrueOnSuccessWithoutThrowing()
    {
        using var transport = new FakeSmartCardTransport();
        transport.EnqueueResponse(0x90, 0x00);
        using var reader = new Acr122uReader(transport);

        var authenticated = await reader.TryAuthenticateAsync(0x04, KeyType.TypeA, KeySlot.Slot0).ConfigureAwait(true);

        Assert.True(authenticated);
    }

    [Fact]
    public async Task TryAuthenticateAsyncReturnsFalseOnFailureWithoutThrowing()
    {
        using var transport = new FakeSmartCardTransport();
        transport.EnqueueResponse(0x63, 0x00);
        using var reader = new Acr122uReader(transport);

        var authenticated = await reader.TryAuthenticateAsync(0x04, KeyType.TypeA, KeySlot.Slot0).ConfigureAwait(true);

        Assert.False(authenticated);
    }

    // ---- §5.5.2 Read Value Block: signed big-endian decoding ---------------------------------------

    [Theory]
    [InlineData("FFFFFFFC", -4)] // §5.5.2 Example 1: Decimal -4 = {FFh, FFh, FFh, FCh}
    [InlineData("00000001", 1)] // §5.5.2 Example 2: Decimal 1 = {00h, 00h, 00h, 01h}
    public async Task ReadValueBlockAsyncDecodesSignedBigEndianValue(string valueHex, int expectedValue)
    {
        using var transport = new FakeSmartCardTransport();
        var raw = Convert.FromHexString(valueHex + "9000");
        transport.EnqueueResponse(raw);
        using var reader = new Acr122uReader(transport);

        var value = await reader.ReadValueBlockAsync(0x05).ConfigureAwait(true);

        Assert.Equal(expectedValue, value);
    }

    // ---- §6.2 LED/Buzzer: SW1=90h-with-variable-SW2 success pattern --------------------------------

    [Fact]
    public async Task SetLedAndBuzzerAsyncExample2DecodesBothLedsOn()
    {
        // Appendix E, Example 2: Response = "90 03h" -> RED and Green LEDs are ON.
        using var transport = new FakeSmartCardTransport();
        transport.EnqueueResponse(0x90, 0x03);
        using var reader = new Acr122uReader(transport);

        var result = await reader.SetLedAndBuzzerAsync(LedBuzzerControlRequest.SetSolid(red: true, green: true)).ConfigureAwait(true);

        Assert.True(result.RedOn);
        Assert.True(result.GreenOn);
    }

    [Fact]
    public async Task SetLedAndBuzzerAsyncExample3DecodesRedOffGreenUnchangedOn()
    {
        // Appendix E, Example 3: Response = "90 02h" -> Green LED not changed (ON), Red LED OFF.
        using var transport = new FakeSmartCardTransport();
        transport.EnqueueResponse(0x90, 0x02);
        using var reader = new Acr122uReader(transport);

        var result = await reader.SetLedAndBuzzerAsync(new LedBuzzerControlRequest { Flags = LedControlFlags.UpdateRedState }).ConfigureAwait(true);

        Assert.False(result.RedOn);
        Assert.True(result.GreenOn);
    }

    [Fact]
    public async Task SetLedAndBuzzerAsyncFailureStatusWordThrows()
    {
        // §6.2's success check is "SW1 == 90h" (SW2 carries LED state, not a pass/fail code), so
        // any SW1 other than 90h must still be treated as failure.
        using var transport = new FakeSmartCardTransport();
        transport.EnqueueResponse(0x63, 0x00);
        using var reader = new Acr122uReader(transport);

        await Assert.ThrowsAsync<Acr122uCommandException>(
            () => reader.SetLedAndBuzzerAsync(LedBuzzerControlRequest.SetSolid(red: true, green: true))).ConfigureAwait(true);
    }
}

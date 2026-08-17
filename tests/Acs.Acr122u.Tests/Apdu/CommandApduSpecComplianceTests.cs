using Acs.Acr122u.Commands;
using Acs.Acr122u.Models;
using Xunit;

namespace Acs.Acr122u.Tests.Apdu;

/// <summary>
/// Every test in this class asserts that a command builder in <see cref="Acr122uCommands"/>
/// produces the exact byte sequence given as a worked example in the ACR122U Application
/// Programming Interface specification (v2.04). Each test's XML doc quotes the spec section and
/// the example text it verifies, so a failing test points straight back to the relevant page.
/// </summary>
public sealed class CommandApduSpecComplianceTests
{
    private static void AssertHex(string expectedHex, byte[] actual)
    {
        ArgumentNullException.ThrowIfNull(expectedHex);
        Assert.Equal(expectedHex.Replace(" ", string.Empty, StringComparison.Ordinal), Convert.ToHexString(actual));
    }

    // ---- §4.1 Get Data (Command Format table; no worked numeric example given in the spec) -----------

    [Fact]
    public void GetUidMatchesCommandFormatTable()
    {
        // §4.1: Get Data, FFh CAh 00h 00h Le=00h ("Full Length") for the UID branch (P1=00h).
        AssertHex("FF CA 00 00 00", Acr122uCommands.GetUid().ToByteArray());
    }

    [Fact]
    public void GetAtsMatchesCommandFormatTable()
    {
        // §4.1: Get Data, FFh CAh 01h 00h Le=00h for the ATS branch (P1=01h).
        AssertHex("FF CA 01 00 00", Acr122uCommands.GetAts().ToByteArray());
    }

    // ---- §5.1 Load Authentication Keys -----------------------------------------------------------

    [Fact]
    public void LoadAuthenticationKeyMatchesSpecExample()
    {
        // §5.1 example: "Load a key {FF FF FF FF FF FFh} into the key location 00h."
        // APDU = {FF 82 00 00h 06 FF FF FF FF FF FFh}
        var key = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        AssertHex("FF 82 00 00 06 FF FF FF FF FF FF", Acr122uCommands.LoadAuthenticationKey(KeySlot.Slot0, key).ToByteArray());
    }

    // ---- §5.2 Authenticate ------------------------------------------------------------------------

    [Fact]
    public void AuthenticateLegacyMatchesSpecExample()
    {
        // §5.2 example 1 (obsolete PC/SC v2.01 format): authenticate block 04h with {Type A, key 00h}.
        // APDU = {FF 88 00 04 60 00h}
        AssertHex("FF 88 00 04 60 00", Acr122uCommands.AuthenticateLegacy(0x04, KeyType.TypeA, KeySlot.Slot0).ToByteArray());
    }

    [Fact]
    public void AuthenticateMatchesSpecExample()
    {
        // §5.2 example 2 (PC/SC v2.07 format): authenticate block 04h with {Type A, key 00h}.
        // APDU = {FF 86 00 00 05 01 00 04 60 00h}
        AssertHex("FF 86 00 00 05 01 00 04 60 00", Acr122uCommands.Authenticate(0x04, KeyType.TypeA, KeySlot.Slot0).ToByteArray());
    }

    // ---- §5.3 Read Binary Blocks -------------------------------------------------------------------

    [Fact]
    public void ReadBinaryBlockMatchesSpecExample1()
    {
        // §5.3 example 1: "Read 16 bytes from the binary block 04h (MIFARE Classic 1K or 4K)"
        // APDU = {FF B0 00 04 10h}
        AssertHex("FF B0 00 04 10", Acr122uCommands.ReadBinaryBlock(0x04, 0x10).ToByteArray());
    }

    [Fact]
    public void ReadBinaryBlockMatchesSpecExample2()
    {
        // §5.3 example 2: "Read 4 bytes from the binary Page 04h (MIFARE Ultralight)"
        // APDU = {FF B0 00 04 04h}
        AssertHex("FF B0 00 04 04", Acr122uCommands.ReadBinaryBlock(0x04, 0x04).ToByteArray());
    }

    // ---- §5.4 Update Binary Blocks -----------------------------------------------------------------

    [Fact]
    public void UpdateBinaryBlockMatchesSpecExample1MifareClassic()
    {
        // §5.4 example 1: update binary block 04h of MIFARE Classic 1K/4K with Data {00 01 .. 0Fh}
        // APDU = {FF D6 00 04 10 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0Fh}
        byte[] data = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F];
        AssertHex(
            "FF D6 00 04 10 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F",
            Acr122uCommands.UpdateBinaryBlock(0x04, data).ToByteArray());
    }

    [Fact]
    public void UpdateBinaryBlockMatchesSpecExample2MifareUltralight()
    {
        // §5.4 example 2: update binary block 04h of MIFARE Ultralight with Data {00 01 02 03}
        // APDU = {FF D6 00 04 04 00 01 02 03h}
        byte[] data = [0x00, 0x01, 0x02, 0x03];
        AssertHex("FF D6 00 04 04 00 01 02 03", Acr122uCommands.UpdateBinaryBlock(0x04, data).ToByteArray());
    }

    // ---- §5.5 Value Block commands (combined worked examples, §5.5.3) ------------------------------

    [Fact]
    public void WriteValueBlockStoreMatchesSpecExample()
    {
        // §5.5.3 combined example 1: "Store a value '1' into block 05h"
        // APDU = {FF D7 00 05 05 00 00 00 00 01h}
        AssertHex(
            "FF D7 00 05 05 00 00 00 00 01",
            Acr122uCommands.WriteValueBlock(0x05, ValueBlockOperation.Store, 1).ToByteArray());
    }

    [Fact]
    public void WriteValueBlockIncrementMatchesSpecExample()
    {
        // §5.5.3 combined example 4: "Increment the value block 05h by '5'"
        // APDU = {FF D7 00 05 05 01 00 00 00 05h}
        AssertHex(
            "FF D7 00 05 05 01 00 00 00 05",
            Acr122uCommands.WriteValueBlock(0x05, ValueBlockOperation.Increment, 5).ToByteArray());
    }

    [Theory]
    [InlineData(-4, "FF FF FF FC")] // §5.5.1 Example 1: Decimal -4 = {FFh, FFh, FFh, FCh}
    [InlineData(1, "00 00 00 01")] // §5.5.1 Example 2: Decimal 1 = {00h, 00h, 00h, 01h}
    public void WriteValueBlockEncodesValueAsBigEndianSignedInt32(int value, string expectedValueHex)
    {
        ArgumentNullException.ThrowIfNull(expectedValueHex);

        var command = Acr122uCommands.WriteValueBlock(0x05, ValueBlockOperation.Store, value);
        var bytes = command.ToByteArray();

        // Command layout is FF D7 00 <block> 05 <VB_OP> <4-byte value>; the value occupies the last 4 bytes.
        var valueBytes = bytes[^4..];
        AssertHex(expectedValueHex, valueBytes);
    }

    [Fact]
    public void RestoreValueBlockMatchesSpecExample()
    {
        // §5.5.3 combined example 3: "Copy the value from value block 05h to value block 06h"
        // APDU = {FF D7 00 05 02 03 06h}
        AssertHex("FF D7 00 05 02 03 06", Acr122uCommands.RestoreValueBlock(0x05, 0x06).ToByteArray());
    }

    [Fact]
    public void ReadValueBlockMatchesCommandFormatTable()
    {
        // §5.5.2's "Read Value Block APDU Format" table (page 19) explicitly documents Le=04h.
        // A later combined worked example (page 20) shows Le=00h instead, which is inconsistent
        // with the table; both pages were rasterized and visually confirmed to genuinely disagree,
        // so this is an inconsistency in the vendor's own document rather than a transcription
        // error on our part. This test follows the authoritative "APDU Format" table (Le=04h).
        AssertHex("FF B1 00 05 04", Acr122uCommands.ReadValueBlock(0x05).ToByteArray());
    }

    // ---- §6.2 Bi-color LED and Buzzer Control (Appendix E worked examples) -------------------------

    [Fact]
    public void SetLedAndBuzzerExample1ReadExistingState()
    {
        // Appendix E, Example 1: read the existing LED state (no flags set, no blink data).
        // APDU = "FF 00 40 00 04 00 00 00 00h"
        var request = new LedBuzzerControlRequest { Flags = LedControlFlags.None };
        AssertHex("FF 00 40 00 04 00 00 00 00", Acr122uCommands.SetLedAndBuzzer(request).ToByteArray());
    }

    [Fact]
    public void SetLedAndBuzzerExample2TurnOnBothLeds()
    {
        // Appendix E, Example 2: turn on RED and Green Color LEDs.
        // APDU = "FF 00 40 0F 04 00 00 00 00h"
        var request = LedBuzzerControlRequest.SetSolid(red: true, green: true);
        AssertHex("FF 00 40 0F 04 00 00 00 00", Acr122uCommands.SetLedAndBuzzer(request).ToByteArray());
    }

    [Fact]
    public void SetLedAndBuzzerExample3TurnOffRedOnly()
    {
        // Appendix E, Example 3: turn off the RED LED only, leaving Green unchanged.
        // APDU = "FF 00 40 04 04 00 00 00 00h"
        // (Not reachable via the SetSolid()/Blink() convenience factories, which always update
        // both LEDs together — constructed directly from flags, exactly as the spec's raw P2 byte
        // requires: UpdateRedState set, RedFinalOn clear, UpdateGreenState/GreenFinalOn untouched.)
        var request = new LedBuzzerControlRequest { Flags = LedControlFlags.UpdateRedState };
        AssertHex("FF 00 40 04 04 00 00 00 00", Acr122uCommands.SetLedAndBuzzer(request).ToByteArray());
    }

    [Fact]
    public void SetLedAndBuzzerExample4BlinkRedOnceForTwoSeconds()
    {
        // Appendix E, Example 4: Red LED on for 2 seconds, buzzer during T1, no repeats after.
        // T1=2000ms=14h, T2=0ms=00h, repeat=01h, buzzer link=01h. APDU = "FF 00 40 50 04 14 00 01 01h"
        var request = LedBuzzerControlRequest.Blink(
            red: true, green: false,
            onDuration: TimeSpan.FromMilliseconds(2000), offDuration: TimeSpan.Zero,
            repeatCount: 1, buzzerLink: BuzzerLink.DuringT1);
        AssertHex("FF 00 40 50 04 14 00 01 01", Acr122uCommands.SetLedAndBuzzer(request).ToByteArray());
    }

    [Fact]
    public void SetLedAndBuzzerExample5BlinkRedThreeTimesAt1Hz()
    {
        // Appendix E, Example 5: Red LED blinks at 1 Hz, three times.
        // T1=500ms=05h, T2=500ms=05h, repeat=03h, buzzer link=01h. APDU = "FF 00 40 50 04 05 05 03 01h"
        var request = LedBuzzerControlRequest.Blink(
            red: true, green: false,
            onDuration: TimeSpan.FromMilliseconds(500), offDuration: TimeSpan.FromMilliseconds(500),
            repeatCount: 3, buzzerLink: BuzzerLink.DuringT1);
        AssertHex("FF 00 40 50 04 05 05 03 01", Acr122uCommands.SetLedAndBuzzer(request).ToByteArray());
    }

    [Fact]
    public void SetLedAndBuzzerExample6BlinkBothLedsSynchronizedAt1Hz()
    {
        // Appendix E, Example 6: Red AND Green LEDs blink together at 1 Hz, three times.
        // T1=05h, T2=05h, repeat=03h, buzzer link=03h (both). APDU = "FF 00 40 F0 04 05 05 03 03h"
        var request = LedBuzzerControlRequest.Blink(
            red: true, green: true,
            onDuration: TimeSpan.FromMilliseconds(500), offDuration: TimeSpan.FromMilliseconds(500),
            repeatCount: 3, buzzerLink: BuzzerLink.DuringT1AndT2);
        AssertHex("FF 00 40 F0 04 05 05 03 03", Acr122uCommands.SetLedAndBuzzer(request).ToByteArray());
    }

    [Fact]
    public void SetLedAndBuzzerExample7BlinkLedsAlternately()
    {
        // Appendix E, Example 7: Red and Green LEDs blink IN TURNS (alternating) at 1 Hz, three
        // times — Red's initial blink state is ON, Green's is OFF. APDU = "FF 00 40 D0 04 05 05 03 01h"
        var request = LedBuzzerControlRequest.Blink(
            red: true, green: true,
            onDuration: TimeSpan.FromMilliseconds(500), offDuration: TimeSpan.FromMilliseconds(500),
            repeatCount: 3, buzzerLink: BuzzerLink.DuringT1,
            redStartsOn: true, greenStartsOn: false);
        AssertHex("FF 00 40 D0 04 05 05 03 01", Acr122uCommands.SetLedAndBuzzer(request).ToByteArray());
    }

    // ---- §6.3 Get Firmware Version ------------------------------------------------------------------

    [Fact]
    public void GetFirmwareVersionMatchesCommandFormatTable()
    {
        // §6.3: "Get Firmware Version" command format: FFh 00h 48h 00h Le=00h.
        AssertHex("FF 00 48 00 00", Acr122uCommands.GetFirmwareVersion().ToByteArray());
    }
}

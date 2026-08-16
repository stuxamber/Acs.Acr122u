namespace Acs.Acr122u.Models;

/// <summary>
/// Bit flags controlling the final and blinking state of the ACR122U's red and green LEDs (§6.2).
/// </summary>
[Flags]
public enum LedControlFlags : byte
{
    /// <summary>No flags set; leave both LEDs' final state unchanged and don't blink.</summary>
    None = 0,

    /// <summary>Final state of the red LED once blinking (if any) completes: on.</summary>
    RedFinalOn = 1 << 0,

    /// <summary>Final state of the green LED once blinking (if any) completes: on.</summary>
    GreenFinalOn = 1 << 1,

    /// <summary>Apply <see cref="RedFinalOn"/> (otherwise the red LED's final state is left unchanged).</summary>
    UpdateRedState = 1 << 2,

    /// <summary>Apply <see cref="GreenFinalOn"/> (otherwise the green LED's final state is left unchanged).</summary>
    UpdateGreenState = 1 << 3,

    /// <summary>The red LED starts its blink cycle on, rather than off.</summary>
    RedInitialBlinkOn = 1 << 4,

    /// <summary>The green LED starts its blink cycle on, rather than off.</summary>
    GreenInitialBlinkOn = 1 << 5,

    /// <summary>Make the red LED blink (requires <see cref="LedBuzzerControlRequest.RepeatCount"/> &gt; 0).</summary>
    BlinkRed = 1 << 6,

    /// <summary>Make the green LED blink (requires <see cref="LedBuzzerControlRequest.RepeatCount"/> &gt; 0).</summary>
    BlinkGreen = 1 << 7,
}

/// <summary>Controls when the buzzer sounds relative to the LED blink duty cycle (§6.2).</summary>
public enum BuzzerLink : byte
{
    /// <summary>The buzzer does not sound.</summary>
    Off = 0x00,

    /// <summary>The buzzer sounds during the T1 (initial blink state) duration.</summary>
    DuringT1 = 0x01,

    /// <summary>The buzzer sounds during the T2 (toggled blink state) duration.</summary>
    DuringT2 = 0x02,

    /// <summary>The buzzer sounds during both T1 and T2.</summary>
    DuringT1AndT2 = 0x03,
}

/// <summary>
/// Describes how the bi-color LED and buzzer should behave, for use with
/// <see cref="Acr122uReader.SetLedAndBuzzerAsync"/> (§6.2). Prefer the
/// <see cref="SetSolid"/> and <see cref="Blink"/> factory methods for the common cases.
/// </summary>
public readonly record struct LedBuzzerControlRequest
{
    /// <summary>The raw LED state/blink control flags.</summary>
    public LedControlFlags Flags { get; init; }

    /// <summary>Duration of the initial blink state (T1), rounded to the nearest 100&#160;ms, up to 25.5 s.</summary>
    public TimeSpan T1Duration { get; init; }

    /// <summary>Duration of the toggled blink state (T2), rounded to the nearest 100&#160;ms, up to 25.5 s.</summary>
    public TimeSpan T2Duration { get; init; }

    /// <summary>Number of times to repeat the T1/T2 blink cycle. Blinking has no effect unless this is greater than zero.</summary>
    public byte RepeatCount { get; init; }

    /// <summary>How the buzzer relates to the blink duty cycle.</summary>
    public BuzzerLink BuzzerLink { get; init; }

    /// <summary>The P2 byte ("LED State Control") sent with the command.</summary>
    public byte StateControlByte => (byte)Flags;

    /// <summary>Builds the 4-byte "Blinking Duration Control" data field sent with the command.</summary>
    internal byte[] ToBytes() =>
    [
        ToUnits(T1Duration),
        ToUnits(T2Duration),
        RepeatCount,
        (byte)BuzzerLink,
    ];

    private static byte ToUnits(TimeSpan span)
    {
        var units = span.TotalMilliseconds / 100d;
        if (units is < 0 or > 255)
        {
            throw new ArgumentOutOfRangeException(
                nameof(span), span, "LED durations must fit in a single 100 ms unit byte (0-25500 ms).");
        }

        return (byte)Math.Round(units, MidpointRounding.AwayFromZero);
    }

    /// <summary>A request that sets both LEDs to a fixed on/off state, with no blinking.</summary>
    public static LedBuzzerControlRequest SetSolid(bool red, bool green) => new()
    {
        Flags = LedControlFlags.UpdateRedState | LedControlFlags.UpdateGreenState
              | (red ? LedControlFlags.RedFinalOn : LedControlFlags.None)
              | (green ? LedControlFlags.GreenFinalOn : LedControlFlags.None),
    };

    /// <summary>
    /// A request that blinks the requested LED(s) at the given rate, a number of times. By
    /// default both requested LEDs start their blink cycle ON, so blinking both together (spec
    /// Appendix E, Example 6) produces a synchronized flash. Pass <paramref name="redStartsOn"/>
    /// / <paramref name="greenStartsOn"/> to override either LED's starting phase — e.g. leaving
    /// one <see langword="false"/> while the other defaults to <see langword="true"/> reproduces
    /// the "blink in turns" alternating pattern from Appendix E, Example 7.
    /// </summary>
    public static LedBuzzerControlRequest Blink(
        bool red,
        bool green,
        TimeSpan onDuration,
        TimeSpan offDuration,
        byte repeatCount,
        BuzzerLink buzzerLink = BuzzerLink.Off,
        bool? redStartsOn = null,
        bool? greenStartsOn = null)
    {
        var flags = LedControlFlags.None;
        if (red)
        {
            flags |= LedControlFlags.BlinkRed;
            if (redStartsOn ?? true)
            {
                flags |= LedControlFlags.RedInitialBlinkOn;
            }
        }

        if (green)
        {
            flags |= LedControlFlags.BlinkGreen;
            if (greenStartsOn ?? true)
            {
                flags |= LedControlFlags.GreenInitialBlinkOn;
            }
        }

        return new LedBuzzerControlRequest
        {
            Flags = flags,
            T1Duration = onDuration,
            T2Duration = offDuration,
            RepeatCount = repeatCount,
            BuzzerLink = buzzerLink,
        };
    }
}

/// <summary>The current LED state reported in SW2 after a §6.2 LED/Buzzer command.</summary>
public readonly record struct LedBuzzerControlResult(bool RedOn, bool GreenOn)
{
    internal static LedBuzzerControlResult FromStatusByte(byte sw2) => new(
        RedOn: (sw2 & 0x01) != 0,
        GreenOn: (sw2 & 0x02) != 0);
}

namespace Acs.Acr122u.Models;

/// <summary>The reader's contactless-chip response timeout (§6.6).</summary>
public readonly record struct CardDetectionTimeout
{
    private readonly byte _rawValue;

    private CardDetectionTimeout(byte rawValue) => _rawValue = rawValue;

    /// <summary>No timeout check is performed (00h).</summary>
    public static readonly CardDetectionTimeout None = new(0x00);

    /// <summary>Wait indefinitely until the contactless chip responds (FFh).</summary>
    public static readonly CardDetectionTimeout Infinite = new(0xFF);

    /// <summary>Creates a timeout expressed as a number of 5-second units (1-254, i.e. 5 s to 1270 s).</summary>
    public static CardDetectionTimeout FromUnits(byte fiveSecondUnits)
    {
        if (fiveSecondUnits == 0xFF)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fiveSecondUnits), fiveSecondUnits, $"Use {nameof(CardDetectionTimeout)}.{nameof(Infinite)} instead of FFh.");
        }

        return new CardDetectionTimeout(fiveSecondUnits);
    }

    /// <summary>Creates the closest representable timeout for the requested <see cref="TimeSpan"/>, rounding up.</summary>
    public static CardDetectionTimeout FromTimeSpan(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            return None;
        }

        var units = Math.Ceiling(timeout.TotalSeconds / 5d);
        return units >= 0xFF ? Infinite : new CardDetectionTimeout((byte)units);
    }

    /// <summary>The raw byte value sent to the reader in P2 of the §6.6 command.</summary>
    public byte ToByte() => _rawValue;
}

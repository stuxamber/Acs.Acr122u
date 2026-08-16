namespace Acs.Acr122u.Transport;

/// <summary>What should happen to the card when a connection is closed (PC/SC <c>dwDisposition</c>).</summary>
public enum SmartCardDisposition : uint
{
    /// <summary>Leave the card powered and in its current state.</summary>
    LeaveCard = 0,

    /// <summary>Reset the card (warm reset).</summary>
    ResetCard = 1,

    /// <summary>Power down the card.</summary>
    UnpowerCard = 2,

    /// <summary>Eject the card, if the reader supports it.</summary>
    EjectCard = 3,
}

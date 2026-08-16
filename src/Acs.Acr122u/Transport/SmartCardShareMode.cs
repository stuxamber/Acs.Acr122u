namespace Acs.Acr122u.Transport;

/// <summary>How exclusively the reader should be opened (PC/SC <c>dwShareMode</c>).</summary>
public enum SmartCardShareMode : uint
{
    /// <summary>This application will NOT allow others to share the reader.</summary>
    Exclusive = 1,

    /// <summary>This application is willing to share the reader with other applications (the common case).</summary>
    Shared = 2,

    /// <summary>Open the reader without powering up or accessing the card, e.g. to send §6.0 escape/control commands.</summary>
    Direct = 3,
}

using Acs.Acr122u.Diagnostics;
using Acs.Acr122u.Transport;

namespace Acs.Acr122u;

/// <summary>Options controlling how <see cref="Acr122uReader"/> connects to and manages the reader.</summary>
public sealed record Acr122uReaderOptions
{
    /// <summary>PC/SC share mode used when connecting. Defaults to <see cref="SmartCardShareMode.Shared"/>.</summary>
    public SmartCardShareMode ShareMode { get; init; } = SmartCardShareMode.Shared;

    /// <summary>How long to wait between polling attempts while waiting for a card to be presented.</summary>
    public TimeSpan CardPollInterval { get; init; } = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Maximum time to wait for a card to be presented before giving up, for the "connect and
    /// wait for a card" methods (<see cref="Acr122uReaderFactory.ConnectFirstAsync"/>,
    /// <see cref="Acr122uReaderFactory.ConnectAsync"/>, <see cref="Acr122uReader.WaitForCardAsync"/>).
    /// <see langword="null"/> (the default) waits indefinitely. If no card appears within this
    /// window, a <see cref="Exceptions.CardNotPresentException"/> is thrown — note that the reader
    /// device being attached over USB is not the same thing as a card/tag sitting on its antenna,
    /// and these methods are specifically waiting for the latter.
    /// </summary>
    public TimeSpan? CardWaitTimeout { get; init; }

    /// <summary>Optional sink for structured diagnostic messages emitted by the reader.</summary>
    public Action<Acr122uLogEntry>? Logger { get; init; }
}

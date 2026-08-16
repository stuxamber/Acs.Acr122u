using Acs.Acr122u.Exceptions;
using Acs.Acr122u.Transport;

namespace Acs.Acr122u;

/// <summary>Convenience helpers for discovering readers and creating a ready-to-use <see cref="Acr122uReader"/>.</summary>
[SupportedOSPlatform("windows")]
public static class Acr122uReaderFactory
{
    /// <summary>Lists the names of every PC/SC reader currently attached, including non-ACR122U readers.</summary>
    public static IReadOnlyList<string> ListReaderNames()
    {
        using var transport = new WinScardTransport();
        return transport.ListReaders();
    }

    /// <summary>Lists only readers whose PC/SC name identifies them as an ACR122U (typically "ACS ACR122U PICC Interface").</summary>
    public static IReadOnlyList<string> ListAcr122uReaderNames() =>
        ListReaderNames().Where(name => name.Contains("ACR122", StringComparison.OrdinalIgnoreCase)).ToArray();

    /// <summary>
    /// Connects to the first attached reader whose name identifies it as an ACR122U, waiting for a
    /// card to be presented, and returns a ready-to-use <see cref="Acr122uReader"/>.
    /// </summary>
    public static async Task<Acr122uReader> ConnectFirstAsync(
        Acr122uReaderOptions? options = null, CancellationToken cancellationToken = default)
    {
        var readers = ListAcr122uReaderNames();
        if (readers.Count == 0)
        {
            throw new CardNotPresentException("No ACR122U reader was found among the attached PC/SC readers.");
        }

        var readerName = readers[0];
        return await ConnectAsync(readerName, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for a card to be presented to the named reader, connects, and returns a ready-to-use
    /// <see cref="Acr122uReader"/>. The caller owns the returned reader and must dispose it.
    /// </summary>
    public static async Task<Acr122uReader> ConnectAsync(
        string readerName, Acr122uReaderOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new Acr122uReaderOptions();
        var transport = new WinScardTransport();
        try
        {
            var loggedWaitMessage = false;
            await transport.ConnectWhenCardPresentAsync(
                readerName,
                options.CardPollInterval,
                options.ShareMode,
                options.CardWaitTimeout,
                onWaiting: _ =>
                {
                    // Log once, on the first miss, rather than every single poll — this is purely
                    // to make it obvious that the call is intentionally waiting for a physical tag
                    // to be placed on the reader, not stuck.
                    if (!loggedWaitMessage)
                    {
                        loggedWaitMessage = true;
                        options.Logger?.Invoke(new Acr122uLogEntry(
                            Acr122uLogLevel.Information,
                            $"Waiting for a card to be presented to '{readerName}'... place an NFC tag on the reader's antenna."));
                    }
                },
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return new Acr122uReader(transport, options, ownsTransport: true);
        }
        catch
        {
            await transport.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}

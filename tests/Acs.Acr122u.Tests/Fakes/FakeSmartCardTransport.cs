using Acs.Acr122u.Apdu;
using Acs.Acr122u.Transport;

namespace Acs.Acr122u.Tests.Fakes;

/// <summary>
/// A minimal test double for <see cref="ISmartCardTransport"/> that never touches real hardware
/// or WinSCard. Responses are queued in advance with <see cref="EnqueueResponse(ResponseApdu)"/>;
/// each call to <see cref="TransmitAsync"/> dequeues the next one (recorded in
/// <see cref="SentCommands"/> for assertions), so tests can exercise <see cref="Acr122uReader"/>'s
/// response parsing and status-word validation exactly as it would run against a real reader.
/// </summary>
internal sealed class FakeSmartCardTransport : ISmartCardTransport
{
    private readonly Queue<ResponseApdu> _responses = new();

    /// <summary>Every command sent through <see cref="TransmitAsync"/>, in order, for assertions.</summary>
    public List<CommandApdu> SentCommands { get; } = [];

    public string? ReaderName { get; private set; } = "Fake ACR122U";

    public bool IsConnected { get; private set; } = true;

    public byte[] Atr { get; set; } = [];

    public IReadOnlyList<string> Readers { get; set; } = ["Fake ACR122U"];

    public void EnqueueResponse(ResponseApdu response) => _responses.Enqueue(response);

    public void EnqueueResponse(params byte[] rawBytes) => _responses.Enqueue(ResponseApdu.Parse(rawBytes));

    public IReadOnlyList<string> ListReaders() => Readers;

    public Task ConnectAsync(string readerName, SmartCardShareMode shareMode = SmartCardShareMode.Shared, CancellationToken cancellationToken = default)
    {
        ReaderName = readerName;
        IsConnected = true;
        return Task.CompletedTask;
    }

    public Task ConnectWhenCardPresentAsync(
        string readerName,
        TimeSpan pollInterval,
        SmartCardShareMode shareMode = SmartCardShareMode.Shared,
        TimeSpan? timeout = null,
        Action<int>? onWaiting = null,
        CancellationToken cancellationToken = default) =>
        ConnectAsync(readerName, shareMode, cancellationToken);

    public Task DisconnectAsync(SmartCardDisposition disposition = SmartCardDisposition.LeaveCard, CancellationToken cancellationToken = default)
    {
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task<ResponseApdu> TransmitAsync(CommandApdu command, CancellationToken cancellationToken = default)
    {
        SentCommands.Add(command);
        if (_responses.Count == 0)
        {
            throw new InvalidOperationException(
                $"No fake response was queued for this TransmitAsync call ({SentCommands.Count} command(s) sent so far). " +
                "Call EnqueueResponse before invoking the method under test.");
        }

        return Task.FromResult(_responses.Dequeue());
    }

    public Task<byte[]> ControlAsync(int controlCode, ReadOnlyMemory<byte> input, CancellationToken cancellationToken = default) =>
        Task.FromResult(Array.Empty<byte>());

    public Task<byte[]> GetAtrAsync(CancellationToken cancellationToken = default) => Task.FromResult(Atr);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public void Dispose() => GC.SuppressFinalize(this);
}

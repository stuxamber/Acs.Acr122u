# Acs.Acr122u

A dependency-free, fully-documented **.NET 10** client library for the **ACS ACR122U USB NFC
Reader**, implementing every command in the official *ACR122U Application Programming Interface*
specification (v2.04) over PC/SC.

- **Complete spec coverage** — general PICC commands (§4), MIFARE Classic read/write/authenticate
  and value-block operations (§5), LED/buzzer control, firmware, PICC operating parameters, timeout
  and antenna control, and contactless-interface status (§6–§7).
- **No external dependencies.** The default transport talks to Windows' built-in WinSCard service
  directly via P/Invoke — nothing to install beyond the .NET 10 runtime.
- **Modern, idiomatic C#.** Nullable reference types, `Span<byte>`/`Memory<byte>`-friendly APIs,
  async all the way down, `SafeHandle`-based native resource management, records/`readonly struct`
  value types, and rich XML documentation cross-referencing the exact section of the specification
  each member implements.
- **Extensible.** Every native call goes through the `ISmartCardTransport` interface — bring your
  own transport (e.g. a cross-platform PC/SC package) to run on Linux/macOS, or wrap it for testing.
- **Safe by default, flexible when needed.** High-level methods validate responses and throw
  descriptive exceptions; `Acr122uCommands` exposes every raw APDU builder for advanced scenarios,
  and `CommandApdu.FromRawBytes` is an escape hatch for anything not yet wrapped.

> This is an independent, unofficial implementation of the publicly documented ACR122U API. It is
> not affiliated with, endorsed by, or sponsored by Advanced Card Systems Ltd. (ACS). See
> [LICENSE](LICENSE) for the full trademark notice.

## Requirements

- .NET 10 SDK or later.
- Windows, with the PC/SC "Smart Card" service running (the default `WinScardTransport` is
  Windows-only — see [Cross-platform use](#cross-platform-use) below).
- An ACR122U reader with the standard Microsoft CCID/PC/SC driver installed (no ACS driver needed).
- To use §6.0 pseudo-APDU commands (LED/buzzer, firmware version, PICC parameters, timeout,
  antenna control, contactless status) **while no card is present**, the PC/SC escape command must
  be enabled for the reader — see **Appendix A** in `docs/ACR122U-API-v2.04.md` or the original
  ACS specification for the registry change (`EscapeCommandEnable = 1` under the reader's device
  parameters key). This is not required when a card is connected and you're sending commands
  through the normal `SCardTransmit` path.

## Quick start

```csharp
using Acs.Acr122u;
using Acs.Acr122u.Models;

// Waits for a card, connects, and returns a ready-to-use reader.
await using var reader = await Acr122uReaderFactory.ConnectFirstAsync();

var uid = await reader.GetUidAsync();
Console.WriteLine($"UID: {Convert.ToHexString(uid)}");

var atr = await reader.GetAtrInfoAsync();
if (atr.Kind is CardKind.MifareClassic1K or CardKind.MifareClassic4K)
{
    byte[] key = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
    await reader.AuthenticateAsync(block: 4, KeyType.TypeA, key);
    var data = await reader.ReadBinaryBlockAsync(4);
    Console.WriteLine(Convert.ToHexString(data));
}

await reader.SetLedAndBuzzerAsync(LedBuzzerControlRequest.SetSolid(red: false, green: true));
```

See `samples/Acs.Acr122u.Sample` for a complete, runnable walkthrough.

## Architecture

```
Acr122uReader           High-level, checked, async API (one method per spec command)
   ↳ Acr122uCommands    Stateless CommandApdu builders — the exact byte layouts from the spec
       ↳ CommandApdu    Immutable APDU value type (Class/Ins/P1/P2/Data/Le → wire bytes)
   ↳ ISmartCardTransport   Abstraction over PC/SC (Transmit / Control / Connect / GetAtr)
       ↳ WinScardTransport Default, dependency-free Windows implementation (P/Invoke to winscard.dll)
```

`Acr122uReaderFactory` ties discovery + connection + construction together for the common case.
For full control (custom share mode, reusing an existing connection, dependency injection, unit
testing with a fake transport), construct `Acr122uReader` directly from any `ISmartCardTransport`.

### Command coverage

| Spec section | Feature | API |
|---|---|---|
| §4.1 | Get UID / Get ATS | `GetUidAsync`, `GetAtsAsync` |
| §5.1 | Load authentication key | `LoadAuthenticationKeyAsync` |
| §5.2 | Authenticate | `AuthenticateAsync`, `TryAuthenticateAsync` |
| §5.3 | Read binary block | `ReadBinaryBlockAsync` |
| §5.4 | Update binary block | `UpdateBinaryBlockAsync` |
| §5.5.1 | Value block operation | `StoreValueAsync`, `IncrementValueAsync`, `DecrementValueAsync` |
| §5.5.2 | Read value block | `ReadValueBlockAsync` |
| §5.5.3 | Restore value block | `RestoreValueBlockAsync` |
| §6.1 | Direct transmit | `DirectTransmitAsync` |
| §6.2 | LED / buzzer control | `SetLedAndBuzzerAsync` |
| §6.3 | Get firmware version | `GetFirmwareVersionAsync` |
| §6.4 / §6.5 | Get/Set PICC operating parameter | `GetPiccOperatingParameterAsync`, `SetPiccOperatingParameterAsync` |
| §6.6 | Set timeout | `SetCardDetectionTimeoutAsync` |
| §6.7 | Buzzer on card detection | `SetBuzzerOnCardDetectionAsync` |
| §7.0 note 1 | Antenna on/off | `SetAntennaAsync` |
| §7.5 | Contactless interface status | `GetContactlessInterfaceStatusAsync` |
| §3.1 | ATR parsing | `GetAtrInfoAsync` / `AtrInfo.Parse` |

MIFARE memory-map helpers (`MifareClassicMemoryMap`, `MifareUltralightMemoryMap`,
`TopazMemoryMap`) turn the sector/block tables in §5.2 and §7.4 into simple method calls instead
of magic numbers.

## Error handling

- `Acr122uCommandException` — the reader returned a non-success status word. Carries the exact
  `CommandApdu` sent and `ResponseApdu` received.
- `CardNotPresentException` — an operation needs a connected card and none is present.
- `WinScardException` — the underlying PC/SC call failed; carries the raw `SCARD_E_*`/`SCARD_W_*`
  code (`ScardErrorCode`) for programmatic handling (e.g. detecting "no smart card" during polling).
- `Acr122uTransportException` — a general transport-level failure.

All of the above derive from `Acr122uException`.

## Cross-platform use

`WinScardTransport` is intentionally the only OS-specific piece, and is annotated
`[SupportedOSPlatform("windows")]`. To run on Linux/macOS, implement `ISmartCardTransport` against
a cross-platform PC/SC package of your choice and pass it to the `Acr122uReader` constructor — the
entire command layer (`Acr122uCommands`, `Acr122uReader`, all models) is pure, portable C# with no
platform dependency.

## Performance notes

- `CommandApdu` is a `readonly struct` that writes directly into caller-supplied buffers
  (`WriteTo(Span<byte>)`) with no intermediate allocations beyond the final byte array handed to
  PC/SC.
- Value-block encoding/decoding uses `System.Buffers.Binary.BinaryPrimitives` for branch-free,
  allocation-free big-endian conversion.
- Native handles are wrapped in `SafeHandle` subclasses for deterministic, GC-safe cleanup even if
  `Dispose` is never called.

## Project layout

```
src/Acs.Acr122u/            The library
samples/Acs.Acr122u.Sample/ A runnable console sample
docs/                       This document and any supplementary notes
```

## Building

```bash
dotnet build src/Acs.Acr122u/Acs.Acr122u.csproj
dotnet run --project samples/Acs.Acr122u.Sample
```

## License

MIT — see [LICENSE](LICENSE). ACR122U is a trademark of Advanced Card Systems Ltd. MIFARE, MIFARE
Classic, MIFARE DESFire and MIFARE Ultralight are registered trademarks of NXP B.V.

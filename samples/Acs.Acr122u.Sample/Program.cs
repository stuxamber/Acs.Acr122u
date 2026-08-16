// A minimal walkthrough of the most common ACR122U operations. Run with:
//   dotnet run --project samples/Acs.Acr122u.Sample
// on Windows, with an ACR122U attached and the PC/SC "Smart Card" service running.

using Acs.Acr122u;
using Acs.Acr122u.Diagnostics;
using Acs.Acr122u.Models;

Console.WriteLine("Looking for an ACR122U reader...");

var options = new Acr122uReaderOptions
{
    Logger = entry => Console.WriteLine(entry),
    CardWaitTimeout = TimeSpan.FromSeconds(30),
};

Console.WriteLine("Place an NFC tag on the reader's antenna if one isn't there already.");

await using var reader = await Acr122uReaderFactory.ConnectFirstAsync(options);
Console.WriteLine($"Connected to '{reader.ReaderName}'.");

var uid = await reader.GetUidAsync();
Console.WriteLine($"UID: {Convert.ToHexString(uid)}");

var atr = await reader.GetAtrInfoAsync();
Console.WriteLine($"Card kind: {atr.Kind}");

var firmware = await reader.GetFirmwareVersionAsync();
Console.WriteLine($"Reader firmware: {firmware}");

// If it's a MIFARE Classic card, authenticate sector 1 (block 4) with the common factory-default
// key and read/print its first data block.
if (atr.Kind is CardKind.MifareClassic1K or CardKind.MifareClassic4K)
{
    byte[] factoryDefaultKey = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
    const byte block = 4;

    await reader.AuthenticateAsync(block, KeyType.TypeA, factoryDefaultKey);
    var data = await reader.ReadBinaryBlockAsync(block);
    Console.WriteLine($"Block {block}: {Convert.ToHexString(data)}");
}

// Blink the green LED three times to signal success.
await reader.SetLedAndBuzzerAsync(LedBuzzerControlRequest.Blink(
    red: false,
    green: true,
    onDuration: TimeSpan.FromMilliseconds(200),
    offDuration: TimeSpan.FromMilliseconds(200),
    repeatCount: 3,
    buzzerLink: BuzzerLink.DuringT1));

Console.WriteLine("Done.");

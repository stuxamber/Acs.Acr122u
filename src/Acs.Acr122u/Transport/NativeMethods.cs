namespace Acs.Acr122u.Transport;

/// <summary>
/// Raw P/Invoke bindings to the Windows Smart Card (WinSCard) API used to talk PC/SC to the
/// reader. Kept internal and deliberately minimal: only what <see cref="WinScardTransport"/>
/// needs. See Appendix A of the ACR122U API specification for the escape/control-code details.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class NativeMethods
{
    internal const int ScardSSuccess = 0;

    // SCARD_E_* / SCARD_W_* values a caller may reasonably want to special-case.
    internal const int ScardENoSmartcard = unchecked((int)0x8010000C);
    internal const int ScardENoReadersAvailable = unchecked((int)0x8010002E);
    internal const int ScardWRemovedCard = unchecked((int)0x80100069);

    internal const uint ScardScopeUser = 0;

    internal const uint ScardShareExclusive = 1;
    internal const uint ScardShareShared = 2;
    internal const uint ScardShareDirect = 3;

    internal const uint ScardProtocolUndefined = 0x0000;
    internal const uint ScardProtocolT0 = 0x0001;
    internal const uint ScardProtocolT1 = 0x0002;
    internal const uint ScardProtocolAny = ScardProtocolT0 | ScardProtocolT1;

    internal const uint ScardLeaveCard = 0;
    internal const uint ScardResetCard = 1;
    internal const uint ScardUnpowerCard = 2;
    internal const uint ScardEjectCard = 3;

    /// <summary>MAX_ATR_SIZE as defined by winsmcrd.h.</summary>
    internal const int MaxAtrSize = 33;

    /// <summary>Windows FILE_DEVICE_SMARTCARD, used to compute vendor escape IOCTLs (Appendix A).</summary>
    private const uint FileDeviceSmartcard = 0x31;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ScardIoRequest
    {
        public uint DwProtocol;
        public uint CbPciLength;
    }

    [DllImport("winscard.dll", SetLastError = true)]
    internal static extern int SCardEstablishContext(uint dwScope, IntPtr pvReserved1, IntPtr pvReserved2, out IntPtr phContext);

    [DllImport("winscard.dll", SetLastError = true)]
    internal static extern int SCardReleaseContext(IntPtr hContext);

    [DllImport("winscard.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    internal static extern int SCardListReadersA(IntPtr hContext, byte[]? mszGroups, byte[]? mszReaders, ref int pcchReaders);

    // CA2101 asks for explicit string marshaling on P/Invoke parameters; the [MarshalAs(LPStr)]
    // below already does exactly that (matching the "A"-suffixed ANSI export we're calling), but
    // the analyzer additionally wants BestFitMapping/ThrowOnUnmappableChar pinned down for the
    // full security-hardened pattern, which DllImport doesn't expose per-parameter — so it's
    // pinned at the DllImport level instead, immediately below.
    [DllImport("winscard.dll", CharSet = CharSet.Ansi, SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    internal static extern int SCardConnectA(
        IntPtr hContext,
        [MarshalAs(UnmanagedType.LPStr)] string szReader,
        uint dwShareMode,
        uint dwPreferredProtocols,
        out IntPtr phCard,
        out uint pdwActiveProtocol);

    [DllImport("winscard.dll", SetLastError = true)]
    internal static extern int SCardDisconnect(IntPtr hCard, uint dwDisposition);

    [DllImport("winscard.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    internal static extern int SCardStatusA(
        IntPtr hCard,
        byte[]? mszReaderNames,
        ref int pcchReaderLen,
        out uint pdwState,
        out uint pdwProtocol,
        byte[] pbAtr,
        ref int pcbAtrLen);

    [DllImport("winscard.dll", SetLastError = true)]
    internal static extern int SCardTransmit(
        IntPtr hCard,
        ref ScardIoRequest pioSendPci,
        byte[] pbSendBuffer,
        int cbSendLength,
        IntPtr pioRecvPci,
        byte[] pbRecvBuffer,
        ref int pcbRecvLength);

    [DllImport("winscard.dll", SetLastError = true)]
    internal static extern int SCardControl(
        IntPtr hCard,
        uint dwControlCode,
        byte[] lpInBuffer,
        int nInBufferSize,
        byte[] lpOutBuffer,
        int nOutBufferSize,
        out int lpBytesReturned);

    /// <summary>
    /// Computes an IOCTL control code the same way the Windows <c>CTL_CODE</c> macro does, for the
    /// vendor "escape" IOCTL described in Appendix A (<c>IOCTL_CCID_ESCAPE = SCARD_CTL_CODE(3500)</c>).
    /// </summary>
    internal static uint ScardCtlCode(int code) => (FileDeviceSmartcard << 16) | ((uint)code << 2);
}

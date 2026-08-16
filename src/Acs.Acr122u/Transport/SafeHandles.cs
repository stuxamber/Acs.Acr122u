namespace Acs.Acr122u.Transport;

[SupportedOSPlatform("windows")]
internal sealed class ScardContextHandle : SafeHandle
{
    internal ScardContextHandle(IntPtr handle)
        : base(IntPtr.Zero, ownsHandle: true) => SetHandle(handle);

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle() =>
        NativeMethods.SCardReleaseContext(handle) == NativeMethods.ScardSSuccess;
}

[SupportedOSPlatform("windows")]
internal sealed class ScardCardHandle : SafeHandle
{
    internal ScardCardHandle(IntPtr handle)
        : base(IntPtr.Zero, ownsHandle: true) => SetHandle(handle);

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle() =>
        NativeMethods.SCardDisconnect(handle, NativeMethods.ScardLeaveCard) == NativeMethods.ScardSSuccess;
}

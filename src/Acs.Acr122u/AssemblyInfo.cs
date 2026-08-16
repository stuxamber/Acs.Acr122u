// Restricts native library probing (winscard.dll) to the trusted System32 directory for every
// P/Invoke in this assembly, rather than the current working directory / application directory,
// mitigating DLL-planting attacks. See CA5392.
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

using System.Runtime.Versioning;

// Declares this executable as Windows-only (it talks to winscard.dll via Acs.Acr122u's default
// transport). Top-level statements can't host assembly-level attributes themselves, so it lives
// here instead. This is what satisfies the platform-compatibility analyzer (CA1416) for the
// calls to Acs.Acr122u's Windows-only APIs in Program.cs.
[assembly: SupportedOSPlatform("windows")]

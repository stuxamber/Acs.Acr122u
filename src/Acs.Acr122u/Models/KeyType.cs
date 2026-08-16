namespace Acs.Acr122u.Models;

/// <summary>MIFARE authentication key type, as used by §5.2 Authentication.</summary>
public enum KeyType : byte
{
    /// <summary>Authenticate using the key as a TYPE A key (60h).</summary>
    TypeA = 0x60,

    /// <summary>Authenticate using the key as a TYPE B key (61h).</summary>
    TypeB = 0x61,
}

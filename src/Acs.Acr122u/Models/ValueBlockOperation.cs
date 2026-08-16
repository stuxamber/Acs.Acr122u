namespace Acs.Acr122u.Models;

/// <summary>The operation to perform on a MIFARE value block (§5.5.1).</summary>
public enum ValueBlockOperation : byte
{
    /// <summary>Format the block as a value block containing the given value.</summary>
    Store = 0x00,

    /// <summary>Increment the value block by the given (non-negative) amount.</summary>
    Increment = 0x01,

    /// <summary>Decrement the value block by the given (non-negative) amount.</summary>
    Decrement = 0x02,
}

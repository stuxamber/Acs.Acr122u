namespace Acs.Acr122u.Diagnostics;

/// <summary>
/// Contactless-interface error codes reported by the PN532 controller inside the ACR122U
/// (Appendix D of the ACR122U API specification). These appear, for example, as the [Err] byte
/// returned by <see cref="Models.ContactlessInterfaceStatus"/> (§7.5).
/// </summary>
public enum Acr122uErrorCode : byte
{
    /// <summary>No error.</summary>
    NoError = 0x00,

    /// <summary>Time out, the target has not answered.</summary>
    TimedOut = 0x01,

    /// <summary>A CRC error has been detected by the contactless UART.</summary>
    CrcError = 0x02,

    /// <summary>A parity error has been detected by the contactless UART.</summary>
    ParityError = 0x03,

    /// <summary>During a MIFARE anti-collision/select operation, an erroneous bit count has been detected.</summary>
    AntiCollisionBitCountError = 0x04,

    /// <summary>Framing error during a MIFARE operation.</summary>
    MifareFramingError = 0x05,

    /// <summary>An abnormal bit collision has been detected during bit-wise anti-collision at 106 Kbps.</summary>
    BitCollisionError = 0x06,

    /// <summary>Communication buffer size insufficient.</summary>
    CommunicationBufferTooSmall = 0x07,

    /// <summary>RF buffer overflow has been detected by the contactless UART.</summary>
    RfBufferOverflow = 0x08,

    /// <summary>In active communication mode, the RF field was not switched on in time by the counterpart.</summary>
    RfFieldActivationTimeout = 0x0A,

    /// <summary>RF protocol error.</summary>
    RfProtocolError = 0x0B,

    /// <summary>Overheating detected; the antenna drivers were automatically switched off.</summary>
    TemperatureError = 0x0D,

    /// <summary>Internal buffer overflow.</summary>
    InternalBufferOverflow = 0x0E,

    /// <summary>Invalid parameter (range, format, ...).</summary>
    InvalidParameter = 0x10,

    /// <summary>DEP protocol: the target does not support the command received from the initiator.</summary>
    UnsupportedDepCommand = 0x12,

    /// <summary>DEP / MIFARE / ISO 14443-4: the data format does not match the specification.</summary>
    DepDataFormatError = 0x13,

    /// <summary>MIFARE authentication error.</summary>
    MifareAuthenticationError = 0x14,

    /// <summary>ISO/IEC 14443-3: UID check byte is wrong.</summary>
    UidCheckByteError = 0x23,

    /// <summary>DEP protocol: invalid device state for the requested operation.</summary>
    InvalidDeviceState = 0x25,

    /// <summary>Operation not allowed in this configuration (host controller interface).</summary>
    OperationNotAllowed = 0x26,

    /// <summary>This command is not acceptable given the current context of the chip.</summary>
    CommandNotAcceptableInCurrentContext = 0x27,

    /// <summary>The chip configured as target has been released by its initiator.</summary>
    TargetReleasedByInitiator = 0x29,

    /// <summary>ISO/IEC 14443-3B only: the responding card's ID does not match the expected card.</summary>
    CardIdMismatch = 0x2A,

    /// <summary>ISO/IEC 14443-3B only: the previously activated card has disappeared.</summary>
    CardDisappeared = 0x2B,

    /// <summary>Mismatch between the NFCID3 initiator and target in DEP 212/424 kbps passive mode.</summary>
    Nfcid3Mismatch = 0x2C,

    /// <summary>An over-current event has been detected.</summary>
    OverCurrent = 0x2D,

    /// <summary>NAD missing in DEP frame.</summary>
    NadMissingInDepFrame = 0x2E,
}

/// <summary>Human-readable descriptions for <see cref="Acr122uErrorCode"/>, taken from Appendix D.</summary>
public static class Acr122uErrorCodeExtensions
{
    /// <summary>Returns the specification's description text for the given error code.</summary>
    public static string GetDescription(this Acr122uErrorCode code) => code switch
    {
        Acr122uErrorCode.NoError => "No error.",
        Acr122uErrorCode.TimedOut => "Time out, the target has not answered.",
        Acr122uErrorCode.CrcError => "A CRC error has been detected by the contactless UART.",
        Acr122uErrorCode.ParityError => "A parity error has been detected by the contactless UART.",
        Acr122uErrorCode.AntiCollisionBitCountError =>
            "During a MIFARE anti-collision/select operation, an erroneous bit count has been detected.",
        Acr122uErrorCode.MifareFramingError => "Framing error during a MIFARE operation.",
        Acr122uErrorCode.BitCollisionError =>
            "An abnormal bit collision has been detected during bit-wise anti-collision at 106 Kbps.",
        Acr122uErrorCode.CommunicationBufferTooSmall => "Communication buffer size insufficient.",
        Acr122uErrorCode.RfBufferOverflow => "RF buffer overflow has been detected by the contactless UART.",
        Acr122uErrorCode.RfFieldActivationTimeout =>
            "In active communication mode, the RF field was not switched on in time by the counterpart.",
        Acr122uErrorCode.RfProtocolError => "RF protocol error.",
        Acr122uErrorCode.TemperatureError =>
            "Overheating detected; the antenna drivers were automatically switched off.",
        Acr122uErrorCode.InternalBufferOverflow => "Internal buffer overflow.",
        Acr122uErrorCode.InvalidParameter => "Invalid parameter (range, format, ...).",
        Acr122uErrorCode.UnsupportedDepCommand =>
            "DEP protocol: the target does not support the command received from the initiator.",
        Acr122uErrorCode.DepDataFormatError =>
            "DEP/MIFARE/ISO 14443-4: the data format does not match the specification.",
        Acr122uErrorCode.MifareAuthenticationError => "MIFARE authentication error.",
        Acr122uErrorCode.UidCheckByteError => "ISO 14443-3: UID check byte is wrong.",
        Acr122uErrorCode.InvalidDeviceState => "DEP protocol: invalid device state for the requested operation.",
        Acr122uErrorCode.OperationNotAllowed =>
            "Operation not allowed in this configuration (host controller interface).",
        Acr122uErrorCode.CommandNotAcceptableInCurrentContext =>
            "This command is not acceptable given the current context of the chip.",
        Acr122uErrorCode.TargetReleasedByInitiator =>
            "The chip configured as target has been released by its initiator.",
        Acr122uErrorCode.CardIdMismatch =>
            "ISO 14443-3B only: the responding card's ID does not match the expected card.",
        Acr122uErrorCode.CardDisappeared => "ISO 14443-3B only: the previously activated card has disappeared.",
        Acr122uErrorCode.Nfcid3Mismatch =>
            "Mismatch between the NFCID3 initiator and target in DEP 212/424 kbps passive mode.",
        Acr122uErrorCode.OverCurrent => "An over-current event has been detected.",
        Acr122uErrorCode.NadMissingInDepFrame => "NAD missing in DEP frame.",
        _ => "Unknown error code.",
    };
}

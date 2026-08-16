namespace Acs.Acr122u.Exceptions;

/// <summary>Thrown when an operation requires a connected card/reader that is not currently available.</summary>
public sealed class CardNotPresentException : Acr122uException
{
    /// <summary>Initializes a new instance with no message.</summary>
    public CardNotPresentException()
    {
    }

    /// <summary>Initializes a new instance with the specified error message.</summary>
    public CardNotPresentException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with the specified error message and inner exception.</summary>
    public CardNotPresentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

namespace Acs.Acr122u.Exceptions;

/// <summary>Thrown when the underlying PC/SC transport fails to establish or maintain a connection.</summary>
public class Acr122uTransportException : Acr122uException
{
    /// <summary>Initializes a new instance with no message.</summary>
    public Acr122uTransportException()
    {
    }

    /// <summary>Initializes a new instance with the specified error message.</summary>
    public Acr122uTransportException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with the specified error message and inner exception.</summary>
    public Acr122uTransportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

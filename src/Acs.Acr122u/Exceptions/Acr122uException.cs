namespace Acs.Acr122u.Exceptions;

/// <summary>Base type for every exception raised by this library.</summary>
public class Acr122uException : Exception
{
    /// <summary>Initializes a new instance with no message.</summary>
    public Acr122uException()
    {
    }

    /// <summary>Initializes a new instance with the specified error message.</summary>
    public Acr122uException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with the specified error message and inner exception.</summary>
    public Acr122uException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

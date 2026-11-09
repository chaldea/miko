namespace Miko.Components.Vnc.Protocol;

/// <summary>A failure of the VNC connection: network, protocol or server refusal.</summary>
public class VncException : Exception
{
    public VncException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>The server sent something this client cannot read (a server bug, or not a VNC server).</summary>
public sealed class VncProtocolException : VncException
{
    public VncProtocolException(string message) : base(message) { }
}

/// <summary>The server refused the connection or the password.</summary>
public sealed class VncAuthenticationException : VncException
{
    public VncAuthenticationException(string message, bool passwordRejected) : base(message) =>
        PasswordRejected = passwordRejected;

    /// <summary>The password was wrong (as opposed to a server that turns the client away, e.g. too many attempts).</summary>
    public bool PasswordRejected { get; }
}

/// <summary>The server needs a password and none was given.</summary>
public sealed class VncPasswordRequiredException : VncException
{
    public VncPasswordRequiredException() : base("The server needs a password.") { }
}

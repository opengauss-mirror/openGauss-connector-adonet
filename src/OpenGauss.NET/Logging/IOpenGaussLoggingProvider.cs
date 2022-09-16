namespace OpenGauss.NET.Logging
{
    /// Used to create logger instances of the given name.
    public interface IOpenGaussLoggingProvider
    {
        /// <summary>
        /// Creates a new IOpenGaussLogger instance of the given name.
        /// </summary>
        OpenGaussLogger CreateLogger(string name);
    }
}

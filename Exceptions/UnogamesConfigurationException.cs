namespace Unogames.Network.Exceptions
{
    /// <summary>
    /// Represents an Unogames.Network configuration exception.
    /// </summary>
    public class UnogamesConfigurationException : UnogamesException
    {
        /// <summary>
        /// Creates a new <see cref="ConfigurationException"/>.
        /// </summary>
        public UnogamesConfigurationException()
            : base("")
        {
        }

        /// <summary>
        /// Creates a new <see cref="ConfigurationException"/>.
        /// </summary>
        /// <param name="message"></param>
        public UnogamesConfigurationException(string message) 
            : base(message)
        {
        }
    }
}

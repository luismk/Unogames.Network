using System;

namespace Unogames.Network.Exceptions
{
    /// <summary>
    /// Represents a generic Unogames.Network exception.
    /// </summary>
    public class UnogamesException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="Exception"/>.
        /// </summary>
        /// <param name="message"></param>
        public UnogamesException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="Exception"/> with an inner exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public UnogamesException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

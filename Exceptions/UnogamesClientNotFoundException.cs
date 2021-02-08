using System;

namespace Unogames.Network.Exceptions
{
    /// <summary>
    /// Represents an Unogames.Network client not found exception.
    /// </summary>
    public class UnogamesClientNotFoundException : UnogamesException
    {
        /// <summary>
        /// Creates a new <see cref="UnogamesClientNotFoundException"/>.
        /// </summary>
        /// <param name="clientId">Client Unique Id</param>
        public UnogamesClientNotFoundException(Guid clientId)
            : base($"Cannot found client {clientId}.")
        {
        }

        /// <summary>
        /// Creates a new <see cref="UnogamesClientNotFoundException"/>.
        /// </summary>
        /// <param name="message"></param>
        public UnogamesClientNotFoundException(string message) 
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new <see cref="UnogamesClientNotFoundException"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public UnogamesClientNotFoundException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}

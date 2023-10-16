using System;

namespace DaveTheMonitor.Achievements
{
    /// <summary>
    /// An exception that is thrown when a texture is missing.
    /// </summary>
    public sealed class TextureMissingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextureMissingException"/> class with a specified texture name.
        /// </summary>
        /// <param name="textureName"></param>
        public TextureMissingException(string textureName) : base($"The texture {textureName} is missing.")
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureMissingException"/> class with a specified texture name and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="textureName"></param>
        public TextureMissingException(string textureName, Exception innerException) : base($"The texture {textureName} is missing.", innerException)
        {

        }
    }
}

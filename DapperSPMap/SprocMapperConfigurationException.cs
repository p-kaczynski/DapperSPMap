using System;

namespace DapperSPMap
{
    public class SprocMapperConfigurationException : Exception
    {
        public SprocMapperConfigurationException()
        {
        }

        public SprocMapperConfigurationException(string message) : base(message)
        {
        }

        public SprocMapperConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
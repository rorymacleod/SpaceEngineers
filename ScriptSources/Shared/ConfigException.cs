using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    public class ConfigException : Exception
    {
        public ConfigException(string message) : base(message) { }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Shared
{
    public static class ConfigExtensions
    {
        public static string GetRequiredString(this MyIni config, string section, string name)
        {
            var value = config.Get(section, name);
            string str = value.ToString();
            if (string.IsNullOrWhiteSpace(str))
            {
                config.Set(section, name, string.Empty);
                throw new ConfigException($"Custom data value \"{name}\" in section \"{section}\" is required.");
            }

            return str;
        }
    }
}

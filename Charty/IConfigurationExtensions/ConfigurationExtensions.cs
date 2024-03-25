using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.CompilerServices;

namespace Charty.ConfigurationExtensions
{
    public static class ConfigurationExtensions
    {
        public static IConfiguration UnbindKey(this IConfiguration configuration, string key)
        {
            var builder = new ConfigurationBuilder();

            foreach (var section in configuration.GetChildren())
            {
                if (!section.Path.Contains(key))
                {
                    builder.AddInMemoryCollection(section.AsEnumerable());
                }
            }

            IConfiguration newConfiguration = builder.Build();
            return newConfiguration;
        }
    }
}

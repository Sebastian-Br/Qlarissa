using Qlarissa.Chart;
using Qlarissa.Chart.ExcludedTimePeriods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qlarissa.CustomConfiguration
{
    public class CustomConfiguration
    {
        public CustomConfiguration()
        {
            // Is initialized from customConfiguration.json
        }

        public Dictionary<string, ExcludedTimePeriod> DefaultExcludedTimePeriods { get; set; }

        /// <summary>
        /// Keys = Symbols
        /// Values = Company names (just make the json easier to understand)
        /// </summary>
        public Dictionary<string, string> SymbolsToBeAnalyzed { get; set; }
    }
}
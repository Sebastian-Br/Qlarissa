using Charty.Chart;
using Charty.Chart.ExcludedTimePeriods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.CustomConfiguration
{
    public class CustomConfiguration
    {
        public CustomConfiguration() { }

        public Dictionary<string, ExcludedTimePeriod> DefaultExcludedTimePeriods { get; set; }

        /// <summary>
        /// Keys = Symbols
        /// Values = Company names (just make the json easier to understand)
        /// </summary>
        public Dictionary<string, string> SymbolsToBeAnalyzed { get; set; }

        /// <summary>
        /// Contains Overview data for Symbols where it can't be retrieved via the API
        /// </summary>
        public Dictionary<string, SymbolOverview> AlternateOverviewSource { get; set; }
    }
}
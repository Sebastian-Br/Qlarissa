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

        public Dictionary<string, ExcludedTimePeriod> DefaultTimePeriodsExcludedFromAnalysis { get; set; }

        public Dictionary<string, ExcludedTimePeriod> DefaultExcludedTimePeriodsForPredictionTargets { get; set; }

        /// <summary>
        /// Keys = Symbols
        /// Values = Company names (just make the json easier to understand)
        /// </summary>
        public Dictionary<string, string> SymbolsToBeAnalyzed { get; set; }

        public void Print()
        {
            PrintExcludedTimePeriods();
        }

        private void PrintExcludedTimePeriods()
        {
            Console.WriteLine(nameof(DefaultTimePeriodsExcludedFromAnalysis) + ":");
            foreach(ExcludedTimePeriod e in DefaultTimePeriodsExcludedFromAnalysis.Values)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine(nameof(DefaultExcludedTimePeriodsForPredictionTargets) + ":");
            foreach (ExcludedTimePeriod e in DefaultExcludedTimePeriodsForPredictionTargets.Values)
            {
                Console.WriteLine(e);
            }
        }
    }
}
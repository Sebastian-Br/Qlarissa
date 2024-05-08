using Qlarissa.Chart.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qlarissa.Chart
{
    public class SymbolOverview
    {
        public SymbolOverview() { }
        public string Symbol { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// e.g. "USD"
        /// </summary>
        public Currency Currency { get; set; }

        public long MarketCapitalization { get; set; }

        public double TrailingPE { get; set; }

        public double ForwardPE { get; set; }

        public double DividendPerShareYearly { get; set; }

        public override string ToString()
        {
            return Name + " (" + Symbol + ")";
        }
    }
}
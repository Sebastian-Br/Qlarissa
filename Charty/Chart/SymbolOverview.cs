using Charty.Chart.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart
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

        public double PEratio { get; set; }

        public double DividentPerShare { get; set; }

        public string GetBasicInformation()
        {
            return Name + " (" + Symbol + ")";
        }

        public void PrintBasicInformation()
        {
            Console.WriteLine(GetBasicInformation());
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart
{
    public class SymbolDataPoint
    {
        public SymbolDataPoint() { }
        public DateOnly Date { get; set; }

        public double LowPrice { get; set; }

        public double MediumPrice { get; set; }

        public double HighPrice { get; set; }
    }
}
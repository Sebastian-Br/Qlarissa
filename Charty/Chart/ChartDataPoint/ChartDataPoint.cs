using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart
{
    public class ChartDataPoint
    {
        public ChartDataPoint() { }
        public DateOnly Date { get; set; }

        public double LowPrice { get; set; }

        public double MediumPrice { get; set; }

        public double HighPrice { get; set; }

        /// <summary>
        /// The index of the first day is 1.
        /// The index of each subsequent day would be 1, 2, 3,... if every day was a trading day
        /// If one day that is not a trading day was between the first and second ChartDataPoint, the second ChartDataPoint
        /// is going to have a DayIndex of 3.
        /// This is important to e.g. create a model of best fit.
        /// </summary>
        public long DayIndex { get; set; }
    }
}
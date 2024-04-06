using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.ChartAnalysis.GrowthVolatilityAnalysis
{
    public class GrowthVolatilityAnalysisSubresult
    {
        public GrowthVolatilityAnalysisSubresult(double growthPercent, double lowestPricePercent) 
        {
            GrowthPercent = growthPercent;
            LowestPricePercent = lowestPricePercent;
        }

        public double GrowthPercent { get; private set; }

        /// <summary>
        /// E.g. if the starting price was 100 in a time frame, and the lowest recorded price within that time frame is 50,
        /// and the end price is 110, then
        /// GrowthPercent = 10
        /// LowestPricePercent = -50
        /// </summary>
        public double LowestPricePercent { get; private set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.Analysis.ExponentialRegression
{
    public class TemporaryEstimates
    {
        public TemporaryEstimates(ExponentialRegressionResult parentResult, double newCurrentPrice)
        {

            CurrentPrice = newCurrentPrice;

            OneYearGrowthEstimatePercentage = (
                (parentResult.Get1YearEstimateAbsolute() + parentResult.AnnualizedDividendPerShare(parentResult.Overview.DividendPerShareYearly))
                / newCurrentPrice
                - 1.0) * 100.0;

            ThreeYearGrowthEstimatePercentage = (
                (parentResult.Get3YearEstimateAbsolute() + parentResult.AnnualizedDividendPerShare(parentResult.Overview.DividendPerShareYearly) + 2.0 * parentResult.Overview.DividendPerShareYearly)
                / newCurrentPrice
                - 1.0) * 100.0;

            //^should this logic be changed? this is using the existing estimates 
        }

        public double CurrentPrice { get; private set; }

        public double OneYearGrowthEstimatePercentage { get; private set; }

        public double ThreeYearGrowthEstimatePercentage { get; private set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.ChartAnalysis
{
    public class RankByExpRegressionResult
    {
        public RankByExpRegressionResult()
        {
            ExponentialRegressionResults = new();
        }

        public List<ExponentialRegressionResult> ExponentialRegressionResults { get; private set;}

        public void PrintResultsRankedBy1YearEstimate()
        {
            OrderBy1YearEstimate();
            int rank = 1;
            Console.WriteLine("****************************************");
            Console.WriteLine("Symbols Ranked by Expected 1 Year Performance");
            Console.WriteLine("****************************************");
            foreach (var result in ExponentialRegressionResults)
            {
                Console.Write("Rank " + rank + ": " + result.Overview.GetBasicInformation());
                if (result.DividendAdjusted)
                {
                    Console.WriteLine("Expected 1 Year Performance (including dividends): " + result.OneYearGrowthEstimatePercentage + " %");
                }
                else
                {
                    Console.WriteLine("Expected 1 Year Performance (excluding dividends): " + result.OneYearGrowthEstimatePercentage + " %");
                }
                rank++;
            }
            Console.WriteLine("****************************************");
        }

        public void PrintResultsRankedBy3YearEstimate()
        {
            OrderBy3YearEstimate();
            int rank = 1;
            Console.WriteLine("****************************************");
            Console.WriteLine("Symbols Ranked by Expected 3 Year Performance");
            Console.WriteLine("****************************************");
            foreach (var result in ExponentialRegressionResults)
            {
                Console.Write("Rank " + rank + ": " + result.Overview.GetBasicInformation());
                if (result.DividendAdjusted)
                {
                    Console.WriteLine("Expected 3 Year Performance (including dividends): " + result.ThreeYearGrowthEstimatePercentage 
                        + " % (1 year equivalent: " + ConvertToOneYearEstimate(result.ThreeYearGrowthEstimatePercentage) + " %)");
                }
                else
                {
                    Console.WriteLine("Expected 3 Year Performance (excluding dividends): " + result.ThreeYearGrowthEstimatePercentage 
                        + " % (1 year equivalent: " + ConvertToOneYearEstimate(result.ThreeYearGrowthEstimatePercentage) + " %)");
                }
                rank++;
            }
            Console.WriteLine("****************************************");
        }

        private void OrderBy1YearEstimate()
        {
            ExponentialRegressionResults.Sort((x, y) => y.OneYearGrowthEstimatePercentage.CompareTo(x.OneYearGrowthEstimatePercentage));
        }

        private void OrderBy3YearEstimate() 
        {
            ExponentialRegressionResults.Sort((x, y) => y.ThreeYearGrowthEstimatePercentage.CompareTo(x.ThreeYearGrowthEstimatePercentage));
        }

        private double ConvertToOneYearEstimate(double threeYearEstimate)
        {
            double rate = threeYearEstimate / 100.0;
            return Math.Round((Math.Pow(1.0 + rate, 1.0 / 3.0) - 1.0) * 100.0, 6); // Rounding to 6 decimal places
        }
    }
}
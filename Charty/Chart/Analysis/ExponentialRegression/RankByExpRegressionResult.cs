using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.Analysis.ExponentialRegression
{
    public class RankByExpRegressionResult
    {
        public RankByExpRegressionResult()
        {
            ExponentialRegressionResults = new();
        }

        public List<ExponentialRegressionResult> ExponentialRegressionResults { get; private set; }

        public void PrintResultsRankedBy1YearEstimate()
        {
            OrderBy1YearEstimate();
            int rank = 1;
            Console.WriteLine("****************************************");
            Console.WriteLine("Symbols Ranked by Expected 1 Year Performance");
            Console.WriteLine("****************************************");
            foreach (var result in ExponentialRegressionResults)
            {
                Console.Write("Rank " + rank + ": " + result.GetExpectedOneYearPerformance_AsText() + "\n");
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
                Console.Write("Rank " + rank + ": " + result.GetExpectedThreeYearPerformance_AsText() + "\n");
                rank++;
            }
            Console.WriteLine("****************************************");
        }

        private void OrderBy1YearEstimate()
        {
            ExponentialRegressionResults.Sort((x, y) => y.GetMostRecent_OneYearGrowthEstimatePercentage().CompareTo(x.GetMostRecent_OneYearGrowthEstimatePercentage()));
        }

        private void OrderBy3YearEstimate()
        {
            ExponentialRegressionResults.Sort((x, y) => y.GetMostRecent_ThreeYearGrowthEstimatePercentage().CompareTo(x.GetMostRecent_ThreeYearGrowthEstimatePercentage()));
        }
    }
}
using Charty.Chart.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.Analysis.GrowthVolatilityAnalyses
{
    internal class LeveragedOverperformanceAnalysisResult
    {
        public LeveragedOverperformanceAnalysisResult(TimePeriod TimePeriod, 
            double averageOverPerformancePercent, double knockoutLikelihoodPercent, double knockoutOrLossLikelihoodPercent,
            double nonLeveragedAvgPerformance, double leveragedAvgPerformance)
        {
            AverageOverPerformancePercent = averageOverPerformancePercent;
            KnockoutLikelihoodPercent = knockoutLikelihoodPercent;
            KnockoutOrLossLikelihoodPercent = knockoutOrLossLikelihoodPercent;
            NonLeveragedAvgPerformance = nonLeveragedAvgPerformance;
            LeveragedAvgPerformance = leveragedAvgPerformance;

            NonLeveragedAvgAnnualizedPerformancePercentage = AnnualizeFactor(NonLeveragedAvgPerformance, TimePeriod);
            LeveragedAvgAnnualizedPerformancePercentage = AnnualizeFactor(LeveragedAvgPerformance, TimePeriod);

            AverageAnnualizedOverPerformancePercent = LeveragedAvgAnnualizedPerformancePercentage - NonLeveragedAvgAnnualizedPerformancePercentage;
        }

        public double AverageOverPerformancePercent { get; private set; }

        /// <summary>
        /// This property is calculated in the constructor and not supplied by the caller.
        /// </summary>
        public double AverageAnnualizedOverPerformancePercent { get; private set; }
        public double KnockoutLikelihoodPercent { get; private set; }
        public double KnockoutOrLossLikelihoodPercent { get; private set; }
        public double NonLeveragedAvgPerformance { get; private set; }

        /// <summary>
        /// This property is calculated in the constructor and not supplied by the caller.
        /// </summary>
        public double NonLeveragedAvgAnnualizedPerformancePercentage { get; private set; }
        public double LeveragedAvgPerformance { get; private set; }

        /// <summary>
        /// This property is calculated in the constructor and not supplied by the caller.
        /// </summary>
        public double LeveragedAvgAnnualizedPerformancePercentage { get; private set; }

        private double AnnualizePercentage(double percentage, TimePeriod TimePeriod)
        {
            double factor = 1.0 + percentage / 100.0;
            double numberOfYears = ((int)TimePeriod) / 12.0;
            return Math.Round((Math.Pow(factor, 1.0 / numberOfYears) - 1.0) * 100.0, 12);
        }

        private double AnnualizeFactor(double factor, TimePeriod TimePeriod)
        {
            double numberOfYears = ((int)TimePeriod) / 12.0;
            return Math.Round((Math.Pow(factor, 1.0 / numberOfYears) - 1.0) * 100.0, 12);
        }
    }
}

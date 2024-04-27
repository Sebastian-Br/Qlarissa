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

            NonLeveragedAvgAnnualizedPerformance = Annualize(NonLeveragedAvgPerformance, TimePeriod);
            LeveragedAvgAnnualizedPerformance = Annualize(LeveragedAvgAnnualizedPerformance, TimePeriod);

            double averageAnnualizedLeveragedOverperformance = LeveragedAvgAnnualizedPerformance / NonLeveragedAvgAnnualizedPerformance;
            AverageAnnualizedOverPerformancePercent = (averageAnnualizedLeveragedOverperformance - 1.0) * 100.0;
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
        public double NonLeveragedAvgAnnualizedPerformance { get; private set; }
        public double LeveragedAvgPerformance { get; private set; }

        /// <summary>
        /// This property is calculated in the constructor and not supplied by the caller.
        /// </summary>
        public double LeveragedAvgAnnualizedPerformance { get; private set; }

        private double Annualize(double percentage, TimePeriod TimePeriod)
        {
            double fraction = 1.0 + percentage / 100.0;
            double numberOfYears = ((int)TimePeriod) / 12.0;
            return Math.Round((Math.Pow(fraction, 1.0 / numberOfYears) - 1.0) * 100.0, 12);
        }
    }
}

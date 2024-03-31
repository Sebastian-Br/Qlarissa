using Charty.Chart.Analysis.CascadingCAGR;
using Charty.Chart.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ScottPlot.Generate;
using static System.Runtime.InteropServices.JavaScript.JSType;
using DateTime = System.DateTime;

namespace Charty.Chart.Analysis.ExponentialRegression
{
    public class ExponentialRegressionResult : IRegressionResult
    {
        /// <summary>
        /// Used when importing data from the DB
        /// </summary>
        /// <param name="_A"></param>
        /// <param name="_B"></param>
        /// <param name="_Overview"></param>
        /// <param name="_CurrentPrice"></param>
        /// <param name="_OneYearGrowthEstimatePercentage"></param>
        /// <param name="_ThreeYearGrowthEstimatePercentage"></param>
        /// <param name="_DateCreated"></param>
        public ExponentialRegressionResult(double _A, double _B, SymbolOverview _Overview, double _CurrentPrice,
            double _OneYearGrowthEstimatePercentage, double _ThreeYearGrowthEstimatePercentage, DateOnly _DateCreated)
        {
            A = _A;
            B = _B;
            Overview = _Overview;
            CurrentPrice = _CurrentPrice;
            OneYearGrowthEstimatePercentage = _OneYearGrowthEstimatePercentage;
            ThreeYearGrowthEstimatePercentage = _ThreeYearGrowthEstimatePercentage;
            DateCreated = _DateCreated;
        }

        public ExponentialRegressionResult(ExponentialRegression e, Symbol symbol)
        {
            A = e.A;
            B = e.B;
            CurrentPrice = symbol.DataPoints.Last().MediumPrice;
            Overview = symbol.Overview;
            DateCreated = DateOnly.FromDateTime(DateTime.Today);
            Rsquared = e.Rsquared;

            if (CurrentPrice <= 0)
            {
                throw new ArgumentException("currentPrice can not be less than or equal to 0");
            }

            SetEstimates();
        }

        public double A { get; private set; }

        public double B { get; private set; }

        public SymbolOverview Overview { get; private set; }

        public double CurrentPrice { get; private set; }

        public double OneYearGrowthEstimatePercentage { get; private set; }

        public RegressionResult RegressionResult { get; private set; } = RegressionResult.Exponential;

        double Rsquared { get; set; }

        public double GetMostRecent_OneYearGrowthEstimatePercentage()
        {
            if(TemporaryEstimates == null)
            {
                return OneYearGrowthEstimatePercentage;
            }

            return TemporaryEstimates.OneYearGrowthEstimatePercentage;
        }

        public double ThreeYearGrowthEstimatePercentage { get; private set; }

        public double GetMostRecent_ThreeYearGrowthEstimatePercentage()
        {
            if (TemporaryEstimates == null)
            {
                return ThreeYearGrowthEstimatePercentage;
            }

            return TemporaryEstimates.ThreeYearGrowthEstimatePercentage;
        }

        public DateOnly DateCreated { get; private set; }

        /// <summary>
        /// This is only used during a session and not saved to the DB.
        /// The user can add a new current price to recalculate estimates.
        /// </summary>
        public TemporaryEstimates TemporaryEstimates { get; private set; }

        public void SetTemporaryEstimates(double newCurrentPrice)
        {
            TemporaryEstimates = new(this, newCurrentPrice);
        }

        public double GetEstimate(double t)
        {
            return A * Math.Pow(B, t);
        }

        public double GetEstimate(DateOnly date)
        {
            double t = date.ToDouble();
            return GetEstimate(t);
        }

        internal double Get1YearEstimateAbsolute()
        {
            DateOnly targetDate = DateCreated.AddYears(1);
            return GetEstimate(targetDate);
        }

        internal double Get3YearEstimateAbsolute()
        {
            DateOnly targetDate = DateCreated.AddYears(3);
            return GetEstimate(targetDate);
        }

        private void SetEstimates()
        {
            OneYearGrowthEstimatePercentage = (
                (Get1YearEstimateAbsolute() + AnnualizedDividendPerShare(Overview.DividendPerShareYearly))
                / CurrentPrice
                - 1.0) * 100.0;

            ThreeYearGrowthEstimatePercentage = (
                (Get3YearEstimateAbsolute() + AnnualizedDividendPerShare(Overview.DividendPerShareYearly) + 2.0 * Overview.DividendPerShareYearly)
                / CurrentPrice
                - 1.0) * 100.0;
        }

        internal double AnnualizedDividendPerShare(double dividendPerSharePerYear)
        {
            DateTime currentDate = DateTime.Now;
            int daysPassed = currentDate.DayOfYear;
            double percentageOfYearPassed = daysPassed / (DateTime.IsLeapYear(currentDate.Year) ? 366.0 : 365.0) * 100.0;

            double annualizedDividend = dividendPerSharePerYear * (1 - percentageOfYearPassed / 100.0);
            return annualizedDividend;
        }

        public string GetExpectedOneYearPerformance_AsText()
        {
            return Overview.GetBasicInformation() + "\n" + "Expected 1 Year Performance: " + GetMostRecent_OneYearGrowthEstimatePercentage() + " % " +
                "\n(Target Date:" + ((TemporaryEstimates == null) ? DateCreated.AddYears(1) : DateOnly.FromDateTime(DateTime.Now).AddYears(1)) + "), Target Price: " + CurrentPrice * (1.0 + OneYearGrowthEstimatePercentage / 100.0);
        }

        public string GetExpectedThreeYearPerformance_AsText()
        {
            return Overview.GetBasicInformation() + "\n" + "Expected 3 Year Performance: " + GetMostRecent_ThreeYearGrowthEstimatePercentage() + " % " +
                "(annualized: " + AnnualizeThreeYearEstimate(GetMostRecent_ThreeYearGrowthEstimatePercentage()) + " %) " +
                "\nTarget Date:" + ((TemporaryEstimates == null) ? DateCreated.AddYears(3) : DateOnly.FromDateTime(DateTime.Now).AddYears(3)) + ", Target Price: " + CurrentPrice * (1.0 + ThreeYearGrowthEstimatePercentage / 100.0);
        }

        private double AnnualizeThreeYearEstimate(double threeYearEstimate)
        {
            double rate = threeYearEstimate / 100.0;
            return Math.Round((Math.Pow(1.0 + rate, 1.0 / 3.0) - 1.0) * 100.0, 6); // Rounding to 6 decimal places
        }

        public override string ToString()
        {
            return "y(t) = " + A + " * " + B + " ^ t";
        }

        public List<double> GetParameters()
        {
            throw new NotImplementedException();
        }

        public double GetRsquared()
        {
            return Rsquared;
        }

        public DateOnly GetCreationDate()
        {
            return DateCreated;
        }

        public RegressionResult GetRegressionResultType()
        {
            return RegressionResult;
        }
    }
}
using Charty.Chart.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.ChartAnalysis
{
    public class ExponentialRegressionResult
    {
        public ExponentialRegressionResult(ExponentialRegression e, double currentPrice, SymbolOverview overview)
        {
            if (currentPrice <= 0)
            {
                throw new ArgumentException("currentPrice can not be less than or equal to 0");
            }

            A = e.A;
            B = e.B;
            CurrentPrice = currentPrice;
            Overview = overview;
            ExpectedDividendPercentage = 0;
            DividendAdjusted = false;
            SetEstimates();
        }

        public double A { get; private set; }

        public double B { get; private set; }

        public double ExpectedDividendPercentage { get; private set; }

        public bool DividendAdjusted { get; private set; }

        public SymbolOverview Overview { get; private set; }

        public double CurrentPrice { get; private set; }

        public double OneYearGrowthEstimatePercentage { get; private set; }

        public double ThreeYearGrowthEstimatePercentage { get; private set; }

        public void SetAnnualDividendPercentage(double dividendPercentage)
        {
            ExpectedDividendPercentage = dividendPercentage;
            DividendAdjusted = true;
            // Recalculate estimates
            SetEstimates();
        }

        public double GetEstimate(DateOnly date)
        {
            return A * Math.Pow(B, ConvertDateToYearIndex(date));
        }

        private double Get1YearEstimate()
        {
            DateOnly todayInOneYear = DateOnly.FromDateTime(DateTime.Today).AddYears(1);
            return A * Math.Pow(B, ConvertDateToYearIndex(todayInOneYear));
        }

        private double Get3YearEstimate()
        {
            DateOnly todayInThreeYears = DateOnly.FromDateTime(DateTime.Today).AddYears(3);
            return A * Math.Pow(B, ConvertDateToYearIndex(todayInThreeYears));
        }

        private void SetEstimates()
        {
            if(!DividendAdjusted)
            {
                OneYearGrowthEstimatePercentage = ((Get1YearEstimate() / CurrentPrice) - 1.0) * 100.0;
                ThreeYearGrowthEstimatePercentage = ((Get3YearEstimate() / CurrentPrice) - 1.0) * 100.0;
            }
            else
            {

            }
        }

        private double ConvertDateToYearIndex(DateOnly date)
        {
            int year = date.Year;
            int dayOfYear = date.DayOfYear;
            int daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
            double yearIndex = year + (dayOfYear / (double)daysInYear);
            return yearIndex;
        }

        private double AnnualizedDividendPerShare(double dividendPerSharePerYear, int year)
        {
            DateTime currentDate = DateTime.Now;
            if(year == currentDate.Year)
            {
                int daysPassed = currentDate.DayOfYear;
                double percentageOfYearPassed = ((double)daysPassed / (DateTime.IsLeapYear(currentDate.Year) ? 366.0 : 365.0)) * 100.0;

                double annualizedDividend = dividendPerSharePerYear * (1 - percentageOfYearPassed / 100.0);
                return annualizedDividend;
            }
            else
            {
                return dividendPerSharePerYear;
            }
        }
    }
}
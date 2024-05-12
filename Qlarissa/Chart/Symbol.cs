using Qlarissa.Chart.Analysis;
using Qlarissa.Chart.Analysis.ExponentialRegression;
using Qlarissa.Chart.Analysis.InverseLogRegression;
using Qlarissa.Chart.ChartAnalysis.GrowthVolatilityAnalysis;
using Qlarissa.Chart.ExcludedTimePeriods;
using Newtonsoft.Json.Linq;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static ScottPlot.Generate;
using static System.Runtime.InteropServices.JavaScript.JSType;
using DateTime = System.DateTime;

namespace Qlarissa.Chart
{
    public class Symbol
    {
        public Symbol(SymbolDataPoint[] dataPoints, SymbolOverview overview, ExponentialRegressionResult exponentialRegressionResult = null)
        {
            DataPoints = dataPoints ?? throw new ArgumentNullException(nameof(dataPoints));
            if (DataPoints.Length == 0)
            {
                throw new ArgumentException("chartDataPoints does not contain any elements!");
            }

            DataPointDateToIndexMap = new();
            int index = 0;
            foreach (SymbolDataPoint dataPoint in DataPoints)
            {
                DataPointDateToIndexMap.Add(dataPoint.Date, index);
                index++;
            }

            Overview = overview ?? throw new ArgumentNullException(nameof(overview));
            TimePeriodsExcludedFromAnalysis = new();
            TimePeriodsExcludedFromPredictionTargets = new();
            ExponentialRegressionModel = exponentialRegressionResult;
        }

        public SymbolOverview Overview { get; private set; }

        public SymbolDataPoint[] DataPoints {  get; private set; }

        public Dictionary<DateOnly, int> DataPointDateToIndexMap { get; private set; }

        public ExponentialRegressionResult ExponentialRegressionModel { get; private set; }

        public InverseLogRegressionResult InverseLogRegressionModel { get; private set; }

        public GrowthVolatilityAnalysis GVA_2Years { get; private set; }
        public GrowthVolatilityAnalysis GVA_1Year { get; private set; }

        Dictionary<string,ExcludedTimePeriod> TimePeriodsExcludedFromAnalysis { get; set; }

        /// <summary>
        /// When analyzing a model's prediction errors,
        /// for asset classes where a sudden change in price does not affect the end result - e.g. by virtue of a barrier being hit -
        /// it makes sense to exclude certain unpredictable events, such as Covid,
        /// such that these events don't yield prediction errors during error correction analysis.
        /// These datapoints can still be used to build a model, but will be skipped when they're the target for error analysis.
        /// </summary>
        Dictionary<string,ExcludedTimePeriod> TimePeriodsExcludedFromPredictionTargets { get; set; }

        bool Analyzed { get; set; }

        public CustomConfiguration.CustomConfiguration CustomConfiguration { get; set; }

        public void RunRegressions_IfNotExists()
        {
            if (!Analyzed)
            {
                ExponentialRegression expR = new ExponentialRegression(this);
                ExponentialRegressionModel = new ExponentialRegressionResult(expR, this);
                InverseLogRegressionModel = new(this);
                GVA_2Years = new(this, Enums.TimePeriod.TwoYears);
                GVA_1Year = new(this, Enums.TimePeriod.OneYear);
                Analyzed = true;
            }
        }

        public override string ToString()
        {
            return Overview.Name + " (" + Overview.Symbol + ") - " + Math.Round(DataPoints.Last().MediumPrice, 2) + " " + Overview.Currency.ToString();
        }

        public bool AddTimePeriodExcludedFromAnalysis(string key, ExcludedTimePeriod excludedTimePeriod)
        {
            return TimePeriodsExcludedFromAnalysis.TryAdd(key, excludedTimePeriod);
        }

        public bool AddTimePeriodExcludedFromPredictionTargets(string key, ExcludedTimePeriod excludedTimePeriod)
        {
            return TimePeriodsExcludedFromPredictionTargets.TryAdd(key, excludedTimePeriod);
        }

        public Dictionary<string, ExcludedTimePeriod> GetTimePeriodsExcludedFromAnalysis()
        {
            return TimePeriodsExcludedFromAnalysis;
        }

        private bool IsDateInExcludedTimePeriod(DateOnly date, ExcludedTimePeriod excludedTimePeriod)
        {
            if (excludedTimePeriod.StartDate == null)
            {
                if (date <= excludedTimePeriod.EndDate)
                {
                    return true;
                }
            }
            else if (excludedTimePeriod.EndDate == null)
            {
                if (date >= excludedTimePeriod.StartDate)
                {
                    return true;
                }
            }
            else if (date <= excludedTimePeriod.EndDate && date >= excludedTimePeriod.StartDate)
            {
                return true;
            }

            return false;
        }

        public bool IsDateValidTargetDateForErrorAnalysis(DateOnly date)
        {
            foreach(var entry in TimePeriodsExcludedFromPredictionTargets.Values)
            {
                if(IsDateInExcludedTimePeriod(date, entry))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsDataPointInExcludedTimePeriods(SymbolDataPoint dataPoint)
        {
            foreach(ExcludedTimePeriod excludedTimePeriod in TimePeriodsExcludedFromAnalysis.Values)
            {
                if(IsDateInExcludedTimePeriod(dataPoint.Date, excludedTimePeriod))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsDateRangeIncludingExcludedTimePeriod(DateOnly startDate, DateOnly endDate, ExcludedTimePeriod excludedTimePeriod)
        {
            if (endDate < startDate)
                throw new Exception("endDate can not be before the startDate!");

            if (startDate < excludedTimePeriod.StartDate && endDate > excludedTimePeriod.EndDate)
            {
                return true;
            }

            return false;
        }

        private bool IsDateRangeExcluded(DateOnly startDate, DateOnly endDate)
        {
            if(endDate < startDate)
                throw new Exception("endDate can not be before the startDate!");

            foreach(ExcludedTimePeriod excludedTimePeriod in TimePeriodsExcludedFromAnalysis.Values)
            {
                if (IsDateInExcludedTimePeriod(startDate, excludedTimePeriod) || // t1 S E t2 + t1 S t2 E
                    IsDateInExcludedTimePeriod(endDate, excludedTimePeriod) ||  // S t1 E t2
                    IsDateRangeIncludingExcludedTimePeriod(startDate, endDate, excludedTimePeriod)) // S t1 t2 E
                {
                    return true;
                }
            }

            return false;
        }

        public SymbolDataPoint[] GetDataPointsForAnalysis()
        {
            List<SymbolDataPoint> result = new List<SymbolDataPoint>();
            for(int i = 0; i < DataPoints.Length; i++)
            {
                if (!IsDataPointInExcludedTimePeriods(DataPoints[i]))
                {
                    result.Add(DataPoints[i]);
                }
            }

            return result.ToArray();
        }

        public SymbolDataPoint[] GetDataPointsForAnalysis_UntilDate(DateOnly date)
        {
            List<SymbolDataPoint> result = new List<SymbolDataPoint>();
            for (int i = 0; i < DataPoints.Length; i++)
            {
                if (DataPoints[i].Date > date)
                {
                    break;
                }

                if (!IsDataPointInExcludedTimePeriods(DataPoints[i]))
                {
                    result.Add(DataPoints[i]);
                }
            }

            return result.ToArray();
        }

        public bool WasStockBelowPrice(double price, DateOnly start, DateOnly end)
        {
            for(int i = 0; i < DataPoints.Length; i++)
            {
                if (DataPoints[i].Date >= start && DataPoints[i].Date <= end)
                {
                    if (DataPoints[i].LowPrice < price)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public double? GetMinimum_NotInExcludedTimePeriods(DateOnly startDate, DateOnly endDate)
        {
            if(IsDateRangeExcluded(startDate, endDate))
            {
                return null;
            }

            return GetMinimum(startDate, endDate);
        }

        private double GetMinimum(DateOnly startDate, DateOnly endDate)
        {
            int startIndex = DataPointDateToIndexMap[startDate];
            int endIndex = DataPointDateToIndexMap[endDate];

            if(endIndex <= startIndex + 1)
            {
                throw new InvalidOperationException("endDate must be at least 2 days after startDate");
            }

            double minimum = DataPoints[startIndex].LowPrice;

            for(int i = startIndex + 1; i <= endIndex; i++)
            {
                if (DataPoints[i].LowPrice < minimum)
                {
                    minimum = DataPoints[i].LowPrice;
                }
            }

            return minimum;
        }

        public double GetNYearForecastAbsolute(double nYearsFromNow)
        {
            double dividends = nYearsFromNow * Overview.DividendPerShareYearly;

            double expRegWeight = ExponentialRegressionModel.GetWeight();
            double invLogRegWeight = InverseLogRegressionModel.GetWeight();

            double totalWeight = expRegWeight + invLogRegWeight;

            double normalized_expRegWeight = expRegWeight / totalWeight;
            double normalized_invLogRegWeight = invLogRegWeight / totalWeight;

            double t = (DateOnly.FromDateTime(DateTime.Now)).ToDouble() + nYearsFromNow;
            double expRegEstimate = ExponentialRegressionModel.GetEstimate(t);
            double invLogEstimate = InverseLogRegressionModel.GetEstimate(t);

            double weighted_expRegEstimate = normalized_expRegWeight * expRegEstimate;
            double weighted_invLogRegWeight = normalized_invLogRegWeight * invLogEstimate;

            double estimate = weighted_expRegEstimate + weighted_invLogRegWeight;

            return estimate + dividends;
        }

        public double GetNYearForecastPercent(double n)
        {
            return ((GetNYearForecastAbsolute(n) / (DataPoints.Last().MediumPrice)) -1)*100.0;
        }

        public void DbgPrintDataPoints()
        {
            foreach(SymbolDataPoint point in DataPoints)
            {
                Console.WriteLine("Date:" + point.Date + " High:" + point.HighPrice + " Low:" + point.LowPrice + " Medium:" + point.MediumPrice);
            }
        }
    }
}
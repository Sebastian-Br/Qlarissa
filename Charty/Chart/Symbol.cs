using Charty.Chart.Analysis;
using Charty.Chart.Analysis.CascadingCAGR;
using Charty.Chart.Analysis.ExponentialRegression;
using Charty.Chart.Analysis.InverseLogRegression;
using Charty.Chart.ChartAnalysis.GrowthVolatilityAnalysis;
using Charty.Chart.ExcludedTimePeriods;
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

namespace Charty.Chart
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
            foreach (SymbolDataPoint dataPoint in dataPoints)
            {
                DataPointDateToIndexMap.Add(dataPoint.Date, index);
                index++;
            }

            Overview = overview ?? throw new ArgumentNullException(nameof(overview));
            ExcludedTimePeriods = new();
            ExponentialRegressionModel = exponentialRegressionResult;
        }

        public SymbolOverview Overview { get; private set; }

        public SymbolDataPoint[] DataPoints {  get; private set; }

        public Dictionary<DateOnly, int> DataPointDateToIndexMap { get; private set; }

        public ExponentialRegressionResult ExponentialRegressionModel { get; private set; }

        public ProjectingCAGR ProjectingCAGRmodel { get; private set; }

        public InverseLogRegressionResult InverseLogRegressionModel { get; private set; }

        public GrowthVolatilityAnalysis GVA_1Year { get; private set; }

        Dictionary<string,ExcludedTimePeriod> ExcludedTimePeriods { get; set; }

        bool Analyzed { get; set; }

        public CustomConfiguration.CustomConfiguration CustomConfiguration { get; set; }

        public void RunRegressions_IfNotExists()
        {
            if (!Analyzed)
            {
                ExponentialRegression expR = new ExponentialRegression(this);
                ExponentialRegressionModel = new ExponentialRegressionResult(expR, this);
                InverseLogRegressionModel = new(this);
                ProjectingCAGRmodel = new(this);
                GVA_1Year = new(this, Enums.TimePeriod.OneYear, CustomConfiguration.SaveDirectoriesConfig.VolatilityAnalysisDirectory);
                Analyzed = true;
            }
        }

        public override string ToString()
        {
            return Overview.Name + " (" + Overview.Symbol + ") - " + Math.Round(DataPoints.Last().MediumPrice, 2) + " " + Overview.Currency.ToString();
        }

        public bool AddExcludedTimePeriod(string key, ExcludedTimePeriod excludedTimePeriod)
        {
            return ExcludedTimePeriods.TryAdd(key, excludedTimePeriod);
        }

        public Dictionary<string, ExcludedTimePeriod> GetExcludedTimePeriods()
        {
            return ExcludedTimePeriods;
        }

        private bool IsDateInExcludedTimePeriod(DateOnly date, ExcludedTimePeriod excludedTimePeriod)
        {
            if(date <= excludedTimePeriod.EndDate && date >= excludedTimePeriod.StartDate)
            {
                return true;
            }

            return false;
        }

        private bool IsDataPointInExcludedTimePeriods(SymbolDataPoint dataPoint)
        {
            foreach(ExcludedTimePeriod excludedTimePeriod in ExcludedTimePeriods.Values)
            {
                if(excludedTimePeriod.StartDate == null)
                {
                    if (dataPoint.Date <= excludedTimePeriod.EndDate)
                    {
                        return true;
                    }
                }
                else if (excludedTimePeriod.EndDate == null)
                {
                    if (dataPoint.Date >= excludedTimePeriod.StartDate)
                    {
                        return true;
                    }
                }
                else if (dataPoint.Date <= excludedTimePeriod.EndDate && dataPoint.Date >= excludedTimePeriod.StartDate)
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

        public bool IsDateRangeExcluded(DateOnly startDate, DateOnly endDate)
        {
            if(endDate < startDate)
                throw new Exception("endDate can not be before the startDate!");

            foreach(ExcludedTimePeriod excludedTimePeriod in ExcludedTimePeriods.Values)
            {
                if(excludedTimePeriod.StartDate == null)
                {
                    if(startDate <= excludedTimePeriod.EndDate)
                    {
                        return true;
                    }
                }
                else if(excludedTimePeriod.EndDate == null)
                {
                    if(endDate >=  excludedTimePeriod.StartDate)
                    {
                        return true;
                    }
                }
                else
                {
                    
                    if (IsDateInExcludedTimePeriod(startDate, excludedTimePeriod) || // t1 S E t2 + t1 S t2 E
                        IsDateInExcludedTimePeriod(endDate, excludedTimePeriod) ||  // S t1 E t2
                        IsDateRangeIncludingExcludedTimePeriod(startDate, endDate, excludedTimePeriod)) // S t1 t2 E
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public SymbolDataPoint[] GetDataPointsNotInExcludedTimePeriods()
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

        public double GetMinimum(DateOnly startDate, DateOnly endDate)
        {
            int startIndex = DataPointDateToIndexMap[startDate];
            int endIndex = DataPointDateToIndexMap[endDate];

            if(endIndex <= startIndex + 1)
            {
                throw new InvalidOperationException("endDate must be at least 2 days after startDate");
            }

            double minimum = DataPoints[startIndex + 1].LowPrice;

            for(int i = startIndex + 2; i < endIndex; i++)
            {
                if (DataPoints[i].LowPrice < minimum)
                {
                    minimum = DataPoints[i].LowPrice;
                }
            }

            return minimum;
        }

        public double GetNYearForecastAbsolute(double n)
        {
            double dividends = n * Overview.DividendPerShareYearly;

            double expRegWeight = ExponentialRegressionModel.GetWeight();
            double pcagrWeight = ProjectingCAGRmodel.GetWeight();
            double invLogRegWeight = InverseLogRegressionModel.GetWeight();

            double totalWeight = expRegWeight + pcagrWeight + invLogRegWeight;

            double normalized_expRegWeight = expRegWeight / totalWeight;
            double normalized_pcagrWeight = pcagrWeight / totalWeight;
            double normalized_invLogRegWeight = invLogRegWeight / totalWeight;

            double t = (DateOnly.FromDateTime(DateTime.Now)).ToDouble() + n;
            double expRegEstimate = ExponentialRegressionModel.GetEstimate(t);
            double pcagrEstimate = ProjectingCAGRmodel.GetEstimate(t);
            double invLogEstimate = InverseLogRegressionModel.GetEstimate(t);

            double weighted_expRegEstimate = normalized_expRegWeight * expRegEstimate;
            double weighted_pcagrWeight = normalized_pcagrWeight * pcagrEstimate;
            double weighted_invLogRegWeight = normalized_invLogRegWeight * invLogEstimate;

            double estimate = weighted_expRegEstimate + weighted_pcagrWeight + weighted_invLogRegWeight;

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
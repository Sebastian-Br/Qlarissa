using Charty.Chart.Analysis.ExponentialRegression;
using Charty.Chart.ExcludedTimePeriods;
using Newtonsoft.Json.Linq;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

            Overview = overview ?? throw new ArgumentNullException(nameof(overview));
            ExcludedTimePeriods = new();
            ExponentialRegressionModel = exponentialRegressionResult;
        }

        public SymbolOverview Overview { get; private set; }

        public SymbolDataPoint[] DataPoints {  get; private set; }

        public ExponentialRegressionResult ExponentialRegressionModel { get; private set; }

        private Dictionary<string,ExcludedTimePeriod> ExcludedTimePeriods { get; set; }

        public void RunExponentialRegression_IfNotExists()
        {
            if(ExponentialRegressionModel == null)
            {
                ExponentialRegressionModel = new ExponentialRegressionResult(new ExponentialRegression(GetDataPointsNotInExcludedTimePeriods()), DataPoints.Last().MediumPrice, Overview);
            }
        }

        public override string ToString()
        {
            if(this.ExponentialRegressionModel != null)
            {
                if (this.ExponentialRegressionModel.TemporaryEstimates != null)
                {
                    return Overview.Name + " (" + Overview.Symbol + ") - " + this.ExponentialRegressionModel.TemporaryEstimates.CurrentPrice + " " + Overview.Currency.ToString();
                }
            }
            return Overview.Name + " (" + Overview.Symbol + ") - " + DataPoints.Last().MediumPrice + " " + Overview.Currency.ToString();
        }

        public bool AddExcludedTimePeriod(string key, ExcludedTimePeriod excludedTimePeriod)
        {
            return ExcludedTimePeriods.TryAdd(key, excludedTimePeriod);
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

        public void Draw(DateOnly start, DateOnly end, string fileName)
        {
            List<double> mediumPricesList = new();

            for(int i = 0; i < DataPoints.Count(); i++)
            {
                if (DataPoints[i].Date >= start && DataPoints[i].Date <= end)
                {
                    mediumPricesList.Add(DataPoints[i].MediumPrice);
                }
            }

            double[] mediumPrices = DataPoints.Select(point => point.MediumPrice).ToArray();
            int numberOfDataPoints = mediumPrices.Length;

            ScottPlot.Plot myPlot = new();
            myPlot.Add.SignalConst(mediumPrices);
            myPlot.SavePng(fileName, 800, 600);
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

        public void DbgPrintDataPoints()
        {
            foreach(SymbolDataPoint point in DataPoints)
            {
                Console.WriteLine("Date:" + point.Date + " High:" + point.HighPrice + " Low:" + point.LowPrice + " Medium:" + point.MediumPrice);
            }
        }
    }
}
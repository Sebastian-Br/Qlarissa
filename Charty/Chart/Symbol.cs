using Charty.Chart.ChartAnalysis;
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
        public Symbol(ChartDataPoint[] chartDataPoints, SymbolOverview chartOverview)
        {
            ChartDataPoints = chartDataPoints ?? throw new ArgumentNullException(nameof(chartDataPoints));
            if (ChartDataPoints.Length == 0)
            {
                throw new ArgumentException("chartDataPoints does not contain any elements!");
            }

            SymbolOverview = chartOverview ?? throw new ArgumentNullException(nameof(chartOverview));
            ExcludedTimePeriods = new();
        }

        public SymbolOverview SymbolOverview { get; private set; }

        public ChartDataPoint[] ChartDataPoints {  get; private set; }

        public ExponentialRegressionResult ExponentialRegressionModel { get; private set; }

        private Dictionary<string,ExcludedTimePeriod> ExcludedTimePeriods { get; set; }

        public void RunExponentialRegression()
        {
            if(ExponentialRegressionModel == null)
            {
                ExponentialRegressionModel = new ExponentialRegressionResult(new ExponentialRegression(GetChartDataPointsWithoutExcludedTimePeriods()), ChartDataPoints.Last().MediumPrice, SymbolOverview);
            }
        }

        public override string ToString()
        {
            return SymbolOverview.Name + " (" + SymbolOverview.Symbol + ") - " + ChartDataPoints.Last().MediumPrice + " " + SymbolOverview.Currency.ToString();
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

        private bool IsDataPointInExcludedTimePeriods(ChartDataPoint dataPoint)
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

        public ChartDataPoint[] GetChartDataPointsWithoutExcludedTimePeriods()
        {
            List<ChartDataPoint> result = new List<ChartDataPoint>();
            for(int i = 0; i < ChartDataPoints.Length; i++)
            {
                if (!IsDataPointInExcludedTimePeriods(ChartDataPoints[i]))
                {
                    result.Add(ChartDataPoints[i]);
                }
            }

            return result.ToArray();
        }

        public void Draw(DateOnly start, DateOnly end, string fileName)
        {
            List<double> mediumPricesList = new();

            for(int i = 0; i < ChartDataPoints.Count(); i++)
            {
                if (ChartDataPoints[i].Date >= start && ChartDataPoints[i].Date <= end)
                {
                    mediumPricesList.Add(ChartDataPoints[i].MediumPrice);
                }
            }

            double[] mediumPrices = ChartDataPoints.Select(point => point.MediumPrice).ToArray();
            int numberOfDataPoints = mediumPrices.Length;

            ScottPlot.Plot myPlot = new();
            myPlot.Add.SignalConst(mediumPrices);
            myPlot.SavePng(fileName, 800, 600);
        }

        public bool WasStockBelowPrice(double price, DateOnly start, DateOnly end)
        {
            for(int i = 0; i < ChartDataPoints.Length; i++)
            {
                if (ChartDataPoints[i].Date >= start && ChartDataPoints[i].Date <= end)
                {
                    if (ChartDataPoints[i].LowPrice < price)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void DbgPrintDataPoints()
        {
            foreach(ChartDataPoint point in ChartDataPoints)
            {
                Console.WriteLine("Date:" + point.Date + " High:" + point.HighPrice + " Low:" + point.LowPrice + " Medium:" + point.MediumPrice);
            }
        }
    }
}
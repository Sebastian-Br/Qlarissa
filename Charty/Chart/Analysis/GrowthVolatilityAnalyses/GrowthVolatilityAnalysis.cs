using Charty.Chart.Enums;
using MathNet.Numerics;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators.TimeUnits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.ChartAnalysis.GrowthVolatilityAnalysis
{
    public class GrowthVolatilityAnalysis
    {
        public GrowthVolatilityAnalysis(Symbol symbol, TimePeriod timePeriod, string saveDirectory = "") 
        {
            Symbol = symbol;
            SymbolDataPoint[] dataPoints = symbol.GetDataPointsNotInExcludedTimePeriods();
            TimePeriod = timePeriod;
            int monthsToLookAhead = (int)TimePeriod;
            Subresults = new();
            LowestMinimumPercentage = 0;
            HighestMinimumPercentage = 0;
            SaveDirectory = saveDirectory;

            foreach (SymbolDataPoint dataPoint in dataPoints)
            {
                try
                {
                    SymbolDataPoint fwdDataPoint = dataPoints.First(x => x.Date >= dataPoint.Date.AddMonths(monthsToLookAhead));
                    double? minimum = symbol.GetMinimum_NotInExcludedTimePeriods(dataPoint.Date, fwdDataPoint.Date);
                    if (minimum == null) // Date range is excluded
                        continue;

                    double temp_LowestPricePercent = ((minimum.Value / dataPoint.HighPrice) - 1.0) * 100.0;
                    if(temp_LowestPricePercent < LowestMinimumPercentage)
                    {
                        LowestMinimumPercentage = temp_LowestPricePercent;
                    }

                    if (temp_LowestPricePercent > HighestMinimumPercentage)
                    {
                        HighestMinimumPercentage = temp_LowestPricePercent;
                    }

                    Subresults.Add(
                        new GrowthVolatilityAnalysisSubresult(
                            growthPercent: ((fwdDataPoint.MediumPrice / dataPoint.MediumPrice) - 1.0) * 100.0,
                            lowestPricePercent: temp_LowestPricePercent)
                        );
                }
                catch (InvalidOperationException e)
                {
                    break; // break out of loop because no fwdDataPoint was found
                }
            }
        }


        public TimePeriod TimePeriod { get; private set; }
        List<GrowthVolatilityAnalysisSubresult> Subresults { get; set; }

        Symbol Symbol { get; set; }
        double LowestMinimumPercentage { get; set; }
        double HighestMinimumPercentage { get; set; }
        string SaveDirectory { get; set; }

        /// <summary>
        /// To get the historic likelihood in % of a -x% drop in the TimePeriod used to initialize this class,
        /// pass -x [%] as an argument.
        /// </summary>
        /// <param name="percentage"></param>
        /// <returns></returns>
        public double GetHistoricLikelihoodOfPercentageDrop(double percentage)
        {
            int count = 0;
            foreach(GrowthVolatilityAnalysisSubresult subResult in  Subresults)
            {
                if(subResult.LowestPricePercent <= percentage)
                {
                    count++;
                }
            }

            double historicLikelihood = ((double)count / (double)Subresults.Count) * 100.0;
            return historicLikelihood;
        }

        public void Draw()
        {
            if (SaveDirectory != "")
            {
                ScottPlot.Plot myPlot = new();
                myPlot.Title(Symbol.Overview.ToString() + " Volatility Analysis");

                int numberOfBars = (int)(HighestMinimumPercentage - LowestMinimumPercentage);
                List<ScottPlot.Bar> bars = new();
                for (int i = 0; i < numberOfBars; i++)
                {
                    double barCenter = 0.5 + i + LowestMinimumPercentage;
                    double rightLimit = barCenter + 0.5;
                    double historicLikelihoodOfPercentageDrop = GetHistoricLikelihoodOfPercentageDrop(rightLimit); // considers all events to the left of rightLimit
                    ScottPlot.Bar myBar = new ScottPlot.Bar() { Position = barCenter, FillColor = Colors.Azure, Value = historicLikelihoodOfPercentageDrop };
                    myBar.Label = myBar.Value.Round(2).ToString();
                    bars.Add(myBar);
                }

                var barPlot = myPlot.Add.Bars(bars.ToArray());
                barPlot.ValueLabelStyle.FontSize = 10;

                myPlot.Axes.Bottom.Label.Text = "Minimum [%] with regards to initial Asset Value over " + (int)TimePeriod + " Month Period (Cumulative)";
                myPlot.Axes.Left.Label.Text = "Likelihood of that Minimum [%]";
                myPlot.SavePng(SaveDirectory + Symbol.Overview.Symbol + "_GVA1Y.png", numberOfBars * 30, 600);
            }
        }
    }
}
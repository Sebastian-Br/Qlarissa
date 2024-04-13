using Charty.Chart.Enums;
using MathNet.Numerics;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators.TimeUnits;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                    double growthPercent = ((fwdDataPoint.MediumPrice / dataPoint.MediumPrice) - 1.0) * 100.0;

                    Subresults.Add(
                        new GrowthVolatilityAnalysisSubresult(
                            growthPercent: growthPercent,
                            lowestPricePercent: temp_LowestPricePercent,
                            startDataPoint: dataPoint,
                            fwdDataPoint: fwdDataPoint)
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
        private double GetHistoricLikelihoodOfPercentageDrop(double percentage)
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

        private double GetHistoricLikelihoodOf_KO_Leverage_Success(double leverage)
        {
            double percentageLossForKnockout = - ((1.0 / leverage) * 100.0);
            int successCount = 0;
            foreach(GrowthVolatilityAnalysisSubresult subResult in Subresults)
            {
                if(percentageLossForKnockout < subResult.LowestPricePercent) // KO-barrier has not been touched
                {
                    successCount++;
                }
            }

            return successCount / (double)Subresults.Count;
        }

        /// <summary>
        /// Returns the historic overperformance in [%] for a Knock-Out-Certificate with a specified leverage.
        /// Positive values indicate overperformance.
        /// Negative values indicate underperformance.
        /// </summary>
        /// <param name="leverage"></param>
        /// <returns>RETURN TYPE MIGHT NEED A SLIGHT REWORK!!!!</returns>
        private (double, double, double) GetHistoric_KO_Leverage_Overperformance_AndKOchance_AndKOorLossChance(double leverage)
        {
            double percentageLossForKnockout = -((1.0 / leverage) * 100.0);
            List<double> leveragedOutcomes = new(); // elements in this list can take values typically ranging from 0 to e.g. 2.0, if the max leveraged return was 2.0x (+100%)
            int koCount = 0;
            int lossCount = 0;

            foreach (GrowthVolatilityAnalysisSubresult subResult in Subresults)
            {
                if (percentageLossForKnockout < subResult.LowestPricePercent) // KO barrier has not been touched
                {
                    double leveragedOutcome = 1.0 + (leverage * subResult.GrowthPercent) / 100.0; // e.g. 1.4 for leverage := 2
                    double nonLeveragedOutcome = 1.0 + subResult.GrowthPercent / 100.0; // e.g. 1.2
                    double overPerformance = leveragedOutcome / nonLeveragedOutcome; // e.g. 1.1666 (16.6% relative overperformance compared to non-leveraged outcome)
                    // leveragedOutcome = -0.009
                    // subResult.LowestPricePercent = -32.358
                    // subResult.GrowthPercent = -36.035
                    leveragedOutcomes.Add(overPerformance);
                    if(leveragedOutcome < 1.0)
                    {
                        lossCount++;
                    }
                }
                else // knock-out event
                {
                    leveragedOutcomes.Add(0.0);
                    koCount++;
                }
            }

            double averageLeveragedOutcomes = leveragedOutcomes.Average();

            double averageOutcomePercent = (averageLeveragedOutcomes - 1.0) * 100.0;
            double knockoutLikelihoodPercent = ((double)koCount) / ((double) Subresults.Count) * 100.0;
            double knockoutOrLossLikelihoodPercent = ((double) (koCount + lossCount)) / ((double) Subresults.Count) * 100.0;

            (double, double, double) result = new(averageOutcomePercent, knockoutLikelihoodPercent, knockoutOrLossLikelihoodPercent);
            return result;
        }

        /// <summary>
        /// This score is more sophisticated than tracking historic min% likelihoods.
        /// Each subresult is assigned a score based the overall growth in that time period and based on min%.
        /// If growth is positive, the score for that subresult is:   growth%² * penalty(min%)^-1
        /// If growth is negative, the score for that subresult is: - growth%² * penalty(min%)
        /// </summary>
        /// <returns></returns>
        public double GetAggregateGrowthVolatilityScore()
        {
            double[] aggregateScores = new double[Subresults.Count];
            int i = 0;

            foreach(GrowthVolatilityAnalysisSubresult subResult in Subresults)
            {
                aggregateScores[i] = 
                    subResult.GrowthPercent > 0 ? 
                    subResult.GrowthPercent * subResult.GrowthPercent * (1.0 / GetMinPercentPenalty(subResult))
                    :
                    -(subResult.GrowthPercent * subResult.GrowthPercent) * GetMinPercentPenalty(subResult);
                i++;
            }

            return aggregateScores.Average();
        }

        private double GetMinPercentPenalty(GrowthVolatilityAnalysisSubresult subResult)
        {
            return
                (
                2.0
                /
                1 + Math.Exp(0.1 * subResult.LowestPricePercent)
                )
                *
                Math.Exp(-0.03 * subResult.LowestPricePercent)
                ;
        }

        public void Draw()
        {
            if (SaveDirectory != "")
            {
                DrawMinPercentGraph();
                DrawLeveragedOverperformanceGraph();
            }
        }

        private void DrawMinPercentGraph()
        {
            ScottPlot.Plot myPlot = new();
            myPlot.Title(Symbol.Overview.ToString() + " Min% Volatility Analysis");

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
            myPlot.SavePng(SaveDirectory + Symbol.Overview.Symbol + "_MINPERCENT_GVA" + (int)TimePeriod + "month.png", numberOfBars * 30, 600);
        }

        private void DrawLeveragedOverperformanceGraph()
        {
            ScottPlot.Plot myPlot = new();
            myPlot.Title(Symbol.Overview.ToString() + " Leveraged Overperformance Analysis");

            double stepSize = 0.1; // do not set this below 0.1. If you do, adjust the barX = Math.Round... bit below.
            double minLeverage = 1.3;
            double maxLeverage = 10.0;
            int numberOfBars = (int) double.Round((maxLeverage - minLeverage) / stepSize);
            List<ScottPlot.Bar> bars = new();
            List<Tick> tickList = new();

            for (int i = 0; i <= numberOfBars; i++)
            {
                double barX = i * stepSize + minLeverage; // barX is the leverage and the actual intended x value of that bar, NOT the position of the bar on the graph
                barX = Math.Round(barX, 1);

                double barPositionX = i;
                (double, double, double) OverPerformanceAndKoChance = GetHistoric_KO_Leverage_Overperformance_AndKOchance_AndKOorLossChance(barX);
                ScottPlot.Bar myBar = new ScottPlot.Bar() { Position = barPositionX, FillColor = Colors.Azure, Value = OverPerformanceAndKoChance.Item1};
                tickList.Add(
                    new(barPositionX, barX.ToString())
                    );
                myBar.Label = myBar.Value.Round(1).ToString() 
                    + "\n" + OverPerformanceAndKoChance.Item2.Round(1).ToString() 
                    + "\n" + OverPerformanceAndKoChance.Item3.Round(1).ToString();

                myBar.LabelOffset = 25f;
                myBar.FillColor = ScottPlot.Color.FromHSL(218, 92, 32, 0.7f);
                bars.Add(myBar);

                var koChanceLine = myPlot.Add.Line(barPositionX, 0, barPositionX, OverPerformanceAndKoChance.Item2);
                koChanceLine.LineColor = Colors.Red;
                koChanceLine.LineWidth = 8.0f;

                var koOrLossLine = myPlot.Add.Line(barPositionX + koChanceLine.LineWidth / 100.0, 0, barPositionX + koChanceLine.LineWidth / 100.0, OverPerformanceAndKoChance.Item3);
                koOrLossLine.LineColor = Colors.Orange;
                koOrLossLine.LineWidth = koChanceLine.LineWidth / 2.0f;
            }

            var barPlot = myPlot.Add.Bars(bars.ToArray());
            barPlot.ValueLabelStyle.FontSize = 10f;

            //https://scottplot.net/cookbook/5.0/CustomizingTicks/RotatedTicksLongLabels/
            myPlot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(tickList.ToArray());
            myPlot.Axes.Bottom.TickLabelStyle.Rotation = 45;
            myPlot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleLeft;

            myPlot.Axes.Bottom.Label.Text = "Leverage";
            myPlot.Axes.Left.Label.Text = "Overperformance vs underlying Asset [%]";
            myPlot.SavePng(SaveDirectory + Symbol.Overview.Symbol + "_LVROVPF_GVA" + (int)TimePeriod + "month.png", numberOfBars * 30, 800);
        }
    }
}
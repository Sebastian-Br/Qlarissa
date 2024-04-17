using Charty.Chart.Enums;
using MathNet.Numerics;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators.TimeUnits;
using System;
using System.Collections.Concurrent;
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

            AdjustmentPercentagePA = 7.5; // todo: set this to the federal interest rate + a risk rate that has to be calculated somehow
        }


        public TimePeriod TimePeriod { get; private set; }
        List<GrowthVolatilityAnalysisSubresult> Subresults { get; set; }

        Symbol Symbol { get; set; }
        double LowestMinimumPercentage { get; set; }
        double HighestMinimumPercentage { get; set; }
        string SaveDirectory { get; set; }

        double AdjustmentPercentagePA { get; set; }

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

        static int GetExactDaysDifference(DateOnly startDate, DateOnly endDate)
        {
            int daysDifference = 0;

            int startYear = startDate.Year;
            int endYear = endDate.Year;

            for (int year = startYear; year < endYear; year++)
            {
                daysDifference += DateTime.IsLeapYear(year) ? 366 : 365;
            }

            daysDifference += endDate.DayOfYear - startDate.DayOfYear;
            return daysDifference;
        }

        double GetAdjustedKObarrier(double koBarrier, DateOnly startDate, DateOnly endDate)
        {
            int numberOfDays = GetExactDaysDifference(startDate, endDate);
            double effectiveAdjustmentPercentage = (AdjustmentPercentagePA / 365.0) * numberOfDays;
            return Math.Pow(koBarrier, 1.0 + effectiveAdjustmentPercentage / 100.0);
        }

        bool WasBarrierHit(double initialBarrier, GrowthVolatilityAnalysisSubresult timeFrame)
        {
            SymbolDataPoint[] dataPoints = Symbol.DataPoints.Where(x => x.Date <= timeFrame.FwdDataPoint.Date && x.Date >= timeFrame.StartDataPoint.Date).ToArray();
            DateOnly initialDate = dataPoints[0].Date;

            foreach(SymbolDataPoint dataPoint in dataPoints)
            {
                double adjustedBarrier = GetAdjustedKObarrier(initialBarrier, initialDate, dataPoint.Date);

                if(dataPoint.LowPrice <= adjustedBarrier)
                {
                    return true;
                }
            }

            return false;
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
            double initialKOfraction = (1.0 - (1.0 / leverage));
            // assuming this is a LONG/CALL product
            List<double> leveragedOutcomes = new(); // elements in this list can take values typically ranging from 0 to e.g. 2.0, if the max leveraged return was 2.0x (+100%)
            int koCount = 0;
            int lossCount = 0;

            foreach (GrowthVolatilityAnalysisSubresult subResult in Subresults)
            {
                double initialKObarrier = initialKOfraction * subResult.StartDataPoint.HighPrice; // pessimistic approach - assuming the asset was bought at the worst moment intra-day
                if (!WasBarrierHit(initialKObarrier, subResult)) // KO barrier has not been touched
                {
                    double leveragedOutcome = 1.0 + (leverage * subResult.GrowthPercent) / 100.0; // e.g. 1.4 for leverage := 2
                    double nonLeveragedOutcome = 1.0 + subResult.GrowthPercent / 100.0; // e.g. 1.2
                    double overPerformance = leveragedOutcome / nonLeveragedOutcome; // e.g. 1.1666 (16.6% relative overperformance compared to non-leveraged outcome)
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
                DrawGrowthAnalysis();
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
                lock(bars)
                {
                    bars.Add(myBar);
                }
            }

            var barPlot = myPlot.Add.Bars(bars.ToArray());
            barPlot.ValueLabelStyle.FontSize = 10;

            myPlot.Axes.Bottom.Label.Text = "Minimum [%] with regards to initial Asset Value over " + (int)TimePeriod + " Month Period (Cumulative)";
            myPlot.Axes.Left.Label.Text = "Likelihood of that Minimum [%]";
            myPlot.SavePng(SaveDirectory + Symbol.Overview.Symbol + "_MINPERCENT_GVA" + (int)TimePeriod + "month.png", numberOfBars * 30, 600);
        }

        /// <summary>
        /// Draws the historic overperformance of open-end LONG/CALL knockout certificates of specific leverages.
        /// Considers the barrier being adjusted daily.
        /// </summary>
        private void DrawLeveragedOverperformanceGraph()
        {
            ScottPlot.Plot myPlot = new();
            myPlot.Title(Symbol.Overview.ToString() + " Leveraged Overperformance Analysis over " + (int)TimePeriod + " months");

            double stepSize = 0.1; // do not set this below 0.1. If you do, adjust the barX = Math.Round... bit below.
            double minLeverage = 1.1;
            double maxLeverage = 7.0;
            int numberOfBars = (int) double.Round((maxLeverage - minLeverage) / stepSize);
            List<ScottPlot.Bar> bars = new();
            List<Tick> tickList = new();

            Parallel.ForEach(Partitioner.Create(0, numberOfBars + 1), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    double barX = i * stepSize + minLeverage; // barX is the leverage and the actual intended x value of that bar, NOT the position of the bar on the graph
                    barX = Math.Round(barX, 1);

                    double barPositionX = i;
                    (double, double, double) OverPerformanceAndKoChance = GetHistoric_KO_Leverage_Overperformance_AndKOchance_AndKOorLossChance(barX);
                    ScottPlot.Bar myBar = new ScottPlot.Bar() { Position = barPositionX, FillColor = Colors.Azure, Value = OverPerformanceAndKoChance.Item1 };

                    lock (tickList)
                    {
                        tickList.Add(new(barPositionX, barX.ToString()));
                    }

                    myBar.Label = myBar.Value.Round(1).ToString()
                        + "\n" + OverPerformanceAndKoChance.Item2.Round(1).ToString()
                        + "\n" + OverPerformanceAndKoChance.Item3.Round(1).ToString();

                    myBar.LabelOffset = 25f;
                    myBar.FillColor = ScottPlot.Color.FromHSL(218, 92, 32, 0.7f);

                    lock (bars)
                    {
                        bars.Add(myBar);
                    }

                    var koChanceLine = myPlot.Add.Line(barPositionX, 0, barPositionX, OverPerformanceAndKoChance.Item2); // should this be locked or not?
                    koChanceLine.LineColor = Colors.Red;
                    koChanceLine.LineWidth = 8.0f;

                    var koOrLossLine = myPlot.Add.Line(barPositionX + koChanceLine.LineWidth / 100.0, 0, barPositionX + koChanceLine.LineWidth / 100.0, OverPerformanceAndKoChance.Item3);
                    koOrLossLine.LineColor = Colors.Orange;
                    koOrLossLine.LineWidth = koChanceLine.LineWidth / 2.0f;
                }
            });

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

        private double Annualize(double percentage)
        {
            double fraction = 1.0 + percentage / 100.0;
            double numberOfYears = ((int)TimePeriod) / 12.0;
            return Math.Round((Math.Pow(fraction, 1.0 / numberOfYears) - 1.0) * 100.0, 12);
        }

        private double DeAnnualize(double percentage)
        {
            double fraction = 1.0 + percentage / 100.0;
            double numberOfYears = ((int)TimePeriod) / 12.0;
            return Math.Round((Math.Pow(fraction, numberOfYears) - 1.0) * 100.0, 12);
        }

        private double GetLikelihoodOfAnnualizedGrowthLessThanPercentage(double percentage)
        {
            double annualizedPercentage = DeAnnualize(percentage);
            return GetLikelihoodOfGrowthLessThanPercentage(annualizedPercentage);
        }

        private double GetLikelihoodOfGrowthLessThanPercentage(double percentage)
        {
            int count = 0;
            foreach(GrowthVolatilityAnalysisSubresult subresult in Subresults)
            {
                if(subresult.GrowthPercent < percentage)
                {
                    count++;
                }
            }

            double result = 
                ((double)count
                /
                (double)Subresults.Count) * 100.0;

            return result;
        }

        private double GetLikelihoodOfAnnualizedGrowthGreaterThanPercentage(double percentage)
        {
            double annualizedPercentage = DeAnnualize(percentage);
            return GetLikelihoodOfGrowthGreaterThanPercentage(annualizedPercentage);
        }

        private double GetLikelihoodOfGrowthGreaterThanPercentage(double percentage)
        {
            int count = 0;
            foreach (GrowthVolatilityAnalysisSubresult subresult in Subresults)
            {
                if (subresult.GrowthPercent > percentage)
                {
                    count++;
                }
            }

            double result =
                ((double)count
                /
                (double)Subresults.Count) * 100.0;

            return result;
        }

        private double GetLikelihoodOfAnnualizedGrowthGreaterThanOrEqualToAndLessThanPercentages(double lPercentage, double hPercentage)
        {
            double annualizedPercentageMin = DeAnnualize(lPercentage);
            double annualizedPercentageMax = DeAnnualize(hPercentage);
            return GetLikelihoodOfGrowthGreaterThanOrEqualToAndLessThanPercentages(annualizedPercentageMin, annualizedPercentageMax);
        }

        private double GetLikelihoodOfGrowthGreaterThanOrEqualToAndLessThanPercentages(double lowerPercentage, double higherPercentage)
        {
            int count = 0;
            foreach (GrowthVolatilityAnalysisSubresult subresult in Subresults)
            {
                if (subresult.GrowthPercent >= lowerPercentage && subresult.GrowthPercent < higherPercentage)
                {
                    count++;
                }
            }

            double result =
                ((double)count
                /
                (double)Subresults.Count) * 100.0;

            return result;
        }

        private void DrawGrowthAnalysis()
        {
            ScottPlot.Plot myPlot = new();
            double averageGrowthInTimePeriod = Subresults.Select(x => x.GrowthPercent).Average();
            myPlot.Title(Symbol.Overview.ToString() + " Annualized Growth Analysis"
                + "\n Average Growth in " + (int)TimePeriod + " months: " + averageGrowthInTimePeriod.Round(2) + "% - Annualized: " + Annualize(averageGrowthInTimePeriod).Round(2) + "%");
            myPlot.Axes.Title.Label.OffsetY = -35;
            double minimum = -30;
            double maximum = 50;
            double stepSize = 5.0;
            int numberOfBars = (int)double.Round((maximum - minimum) / stepSize);
            List<ScottPlot.Bar> bars = new();
            List<Tick> tickList = new();

            ScottPlot.Bar initialBar = new ScottPlot.Bar() { Position = -1, FillColor = Colors.Azure, Value = GetLikelihoodOfAnnualizedGrowthLessThanPercentage(minimum) };
            tickList.Add(
                new(-1, "<" + minimum + "%")
                );
            initialBar.Label = initialBar.Value.Round(1).ToString();

            initialBar.LabelOffset = 10f;
            bars.Add(initialBar);

            Parallel.ForEach(Partitioner.Create(0, numberOfBars), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    double barX = i * stepSize + minimum;
                    barX = Math.Round(barX, 1);

                    double barPositionX = i;
                    ScottPlot.Bar myBar = new ScottPlot.Bar()
                    {
                        Position = barPositionX,
                        FillColor = Colors.Azure,
                        Value = GetLikelihoodOfAnnualizedGrowthGreaterThanOrEqualToAndLessThanPercentages(barX, barX + stepSize)
                    };

                    lock (tickList)
                    {
                        tickList.Add(
                        new(barPositionX, barX + "<=%<" + (barX + stepSize))
                        );
                    }
                    
                    myBar.Label = myBar.Value.Round(1).ToString();

                    myBar.LabelOffset = 10f;
                    lock (bars)
                    {
                        bars.Add(myBar);
                    }
                }
            });

            ScottPlot.Bar lastBar = new ScottPlot.Bar() { Position = numberOfBars, FillColor = Colors.Azure, Value = GetLikelihoodOfAnnualizedGrowthGreaterThanPercentage(maximum) };
            tickList.Add(
                new(numberOfBars, ">" + maximum + "%")
                );
            lastBar.Label = lastBar.Value.Round(1).ToString();

            lastBar.LabelOffset = 10f;
            bars.Add(lastBar);

            var barPlot = myPlot.Add.Bars(bars.ToArray());
            barPlot.ValueLabelStyle.FontSize = 10f;

            //https://scottplot.net/cookbook/5.0/CustomizingTicks/RotatedTicksLongLabels/
            myPlot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(tickList.ToArray());
            myPlot.Axes.Bottom.TickLabelStyle.Rotation = 45;
            myPlot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleLeft;

            float largestLabelWidth = 0;
            foreach (Tick tick in tickList)
            {
                PixelSize size = myPlot.Axes.Bottom.TickLabelStyle.Measure(tick.Label);
                largestLabelWidth = Math.Max(largestLabelWidth, size.Width);
            }

            // ensure axis panels do not get smaller than the largest label
            myPlot.Axes.Bottom.MinimumSize = largestLabelWidth + 30;
            myPlot.Axes.Right.MinimumSize = largestLabelWidth;

            myPlot.Axes.Bottom.Label.Text = "Annualized Growth [%] over " + (int)TimePeriod + " month period";
            myPlot.Axes.Left.Label.Text = "Likelihood of Growth [%]";
            myPlot.SavePng(SaveDirectory + Symbol.Overview.Symbol + "_GrowthAnalysis_" + (int)TimePeriod + "month.png", numberOfBars * 45, 500);
        }

        /*
         Q: Why does this result not match the CAGR for the total duration of the analysis?

        A: This is not a bug. Consider this example.
        t 1	    2	3	4	5
        p 100	110	121	133	146

        growth = 46%
        cagr = 7.86%

        sliding windows (2):
        121 / 100 = 21%
        133 / 110 = 20.91%
        146 / 121 = 20.66%
        Avg Sliding windows: 20.856%
        Annualized Sliding Window: ~9.92%

        Merely taking the average of sliding windows of any length will usually not yield the cagr,
        and the cagr is not very predictive of YoY or 2YoY growth, etc., as that is not its purpose
         */
    }
}
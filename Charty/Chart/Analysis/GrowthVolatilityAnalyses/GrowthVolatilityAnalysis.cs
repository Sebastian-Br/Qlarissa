using Charty.Chart.Analysis.GrowthVolatilityAnalyses;
using Charty.Chart.Enums;
using Charty.CustomConfiguration;
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
        public GrowthVolatilityAnalysis(Symbol symbol, TimePeriod timePeriod) 
        {
            Symbol = symbol;
            SymbolDataPoint[] dataPoints = symbol.GetDataPointsNotInExcludedTimePeriods();
            TimePeriod = timePeriod;
            int monthsToLookAhead = (int)TimePeriod;
            Subresults = new();
            LowestMinimumPercentage = 0;
            HighestMinimumPercentage = 0;

            foreach (SymbolDataPoint dataPoint in dataPoints)
            {
                try
                {
                    SymbolDataPoint fwdDataPoint = dataPoints.First(x => x.Date >= dataPoint.Date.AddMonths(monthsToLookAhead));
                    double? minimum = symbol.GetMinimum_NotInExcludedTimePeriods(dataPoint.Date, fwdDataPoint.Date);
                    if (minimum is null) // Date range is excluded
                        continue;

                    double temp_LowestPricePercent = ((minimum.Value / dataPoint.HighPrice) - 1.0) * 100.0;

                    if(temp_LowestPricePercent < LowestMinimumPercentage) // these are set to know the upper/lower boundaries between which to draw the max-unrealized-loss graph
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
                            maximumUnrealizedLoss: temp_LowestPricePercent,
                            startDataPoint: dataPoint,
                            fwdDataPoint: fwdDataPoint)
                        );
                }
                catch (InvalidOperationException e)
                {
                    break; // break out of loop because no fwdDataPoint was found
                }
            }

            AdjustmentPercentagePA = 9.93; // todo: set this to the federal interest rate + a risk rate that has to be calculated somehow
        }


        public TimePeriod TimePeriod { get; private set; }
        List<GrowthVolatilityAnalysisSubresult> Subresults { get; set; }

        Symbol Symbol { get; set; }
        double LowestMinimumPercentage { get; set; }
        double HighestMinimumPercentage { get; set; }
        double AdjustmentPercentagePA { get; set; }

        /// <summary>
        /// To get the historic likelihood in % of a -x% drop in the TimePeriod used to initialize this class,
        /// pass -x [%] as an argument.
        /// </summary>
        /// <param name="percentage"></param>
        /// <returns></returns>
        private double GetHistoricLikelihoodOfMaximumUnrealizedLoss_GreaterThanOrEqualTo(double percentage)
        {
            int count = 0;
            foreach(GrowthVolatilityAnalysisSubresult subResult in  Subresults)
            {
                if(subResult.MaximumUnrealizedLoss >= percentage)
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
            double currentKoBarrier = koBarrier;

            for(int i = 0; i < numberOfDays; i++)
            {
                currentKoBarrier = ProgressKoBarrierByOneDay(currentKoBarrier);
            }

            return currentKoBarrier;
        }

        double ProgressKoBarrierByOneDay(double previousKoBarrier)
        {
            return (previousKoBarrier * (1.0 + (AdjustmentPercentagePA / 100.0) / 360.0)).Round(4); // 360 is used in banking instead of 365. e.g. see https://derivate.bnpparibas.com/MediaLibrary/Document/Backend/Derivative_Documents/OfferCondition/FT_ISS_DE.2023-04-25.60919_call-non-Italian-001.pdf
        }

        bool WasBarrierHit(double initialBarrier, GrowthVolatilityAnalysisSubresult timeFrame)
        {
            SymbolDataPoint[] dataPoints = Symbol.DataPoints.Where(x => x.Date <= timeFrame.FwdDataPoint.Date && x.Date >= timeFrame.StartDataPoint.Date).ToArray();
            DateOnly initialDate = dataPoints[0].Date;
            double effectiveBarrier = initialBarrier;
            DateOnly lastDate = initialDate;

            foreach(SymbolDataPoint dataPoint in dataPoints)
            {
                //effectiveBarrier = GetAdjustedKObarrier(initialBarrier, initialDate, dataPoint.Date); // O(n²). My condolences; the performance just died.

                int daysSinceLastDate = GetExactDaysDifference(lastDate, dataPoint.Date);
                for (int d = 0; d < daysSinceLastDate; d++)
                {
                    effectiveBarrier = ProgressKoBarrierByOneDay(effectiveBarrier);
                }

                if (dataPoint.LowPrice <= effectiveBarrier)
                {
                    return true;
                }

                lastDate = dataPoint.Date;
            }

            return false;
        }

        private LeveragedOverperformanceAnalysisResult GetHistoricLeveragedOverperformanceAnalysis(double leverage)
        {
            double initialKOfraction = (1.0 - (1.0 / leverage));
            int koCount = 0;
            int lossCount = 0;

            List<double> nonLeveragedOutcomes = new();
            List<double> leveragedOutcomes = new();

            foreach (GrowthVolatilityAnalysisSubresult subResult in Subresults)
            {
                double initialKObarrier = initialKOfraction * subResult.StartDataPoint.HighPrice; // pessimistic approach - assuming the asset was bought at the worst moment intra-day
                double nonLeveragedOutcome = 1.0 + subResult.GrowthPercent / 100.0; // e.g. 1.2
                nonLeveragedOutcomes.Add(nonLeveragedOutcome);

                if (!WasBarrierHit(initialKObarrier, subResult)) // KO barrier has not been touched
                {
                    //double leveragedOutcome = 1.0 + (leverage * subResult.GrowthPercent) / 100.0; // e.g. 1.4 for leverage := 2
                    // TODO: The above calculation is incorrect. The price of a KO certificate is equal to (Underlying Price - KO-Barrier) * 0.1
                    double initialPriceOfCertificate = (subResult.StartDataPoint.MediumPrice - initialKObarrier) * 0.1;
                    double fwdKoBarrier = GetAdjustedKObarrier(initialKObarrier, subResult.StartDataPoint.Date, subResult.FwdDataPoint.Date);
                    double forwardPriceOfCertificate = (subResult.FwdDataPoint.MediumPrice - fwdKoBarrier) * 0.1;
                    double leveragedOutcome = forwardPriceOfCertificate / initialPriceOfCertificate;
                    leveragedOutcomes.Add(leveragedOutcome);

                    if (leveragedOutcome < 1.0)
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

            double nonLeveragedAvgPerformance = nonLeveragedOutcomes.Average();
            double leveragedAvgPerformance = leveragedOutcomes.Average();

            double averageLeveragedOverperformance = leveragedAvgPerformance / nonLeveragedAvgPerformance;
            double averageOverPerformancePercent = (averageLeveragedOverperformance - 1.0) * 100.0;
            double knockoutLikelihoodPercent = ((double)koCount) / ((double)Subresults.Count) * 100.0;
            double knockoutOrLossLikelihoodPercent = ((double)(koCount + lossCount)) / ((double)Subresults.Count) * 100.0;

            LeveragedOverperformanceAnalysisResult result = new(TimePeriod, averageOverPerformancePercent, knockoutLikelihoodPercent, knockoutOrLossLikelihoodPercent,
                nonLeveragedAvgPerformance, leveragedAvgPerformance);
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
                1 + Math.Exp(0.1 * subResult.MaximumUnrealizedLoss)
                )
                *
                Math.Exp(-0.03 * subResult.MaximumUnrealizedLoss)
                ;
        }

        public void Draw()
        {
            DrawMaxLossGraph();
            DrawLeveragedOverperformanceGraph();
            DrawGrowthAnalysis();
        }

        private void DrawMaxLossGraph()
        {
            ScottPlot.Plot myPlot = new();
            myPlot.Title(Symbol.Overview.ToString() + " Minimum Sustained Value [%] - " + (int)TimePeriod + " months. P(-X%) = Chance of MSV being >= -X%"
                + "\n" + "P(-10%)=" + GetHistoricLikelihoodOfMaximumUnrealizedLoss_GreaterThanOrEqualTo(-10).Round(2) + "% P(-20%)=" + GetHistoricLikelihoodOfMaximumUnrealizedLoss_GreaterThanOrEqualTo(-20).Round(2) 
                + "% P(-30%)=" + GetHistoricLikelihoodOfMaximumUnrealizedLoss_GreaterThanOrEqualTo(-30).Round(2) + "% P(-40%)=" + GetHistoricLikelihoodOfMaximumUnrealizedLoss_GreaterThanOrEqualTo(-40).Round(2)
                + "% P(-50%)=" + GetHistoricLikelihoodOfMaximumUnrealizedLoss_GreaterThanOrEqualTo(-50).Round(2)
                +"%");
            myPlot.Axes.Title.Label.OffsetY = -35;
            int numberOfBars = (int)(HighestMinimumPercentage - LowestMinimumPercentage);
            List<ScottPlot.Bar> bars = new();
            for (int i = 0; i < numberOfBars; i++)
            {
                double barCenter = 0.5 + i + LowestMinimumPercentage;
                double rightLimit = barCenter + 0.5;
                double historicLikelihoodOfPercentageDrop = GetHistoricLikelihoodOfMaximumUnrealizedLoss_GreaterThanOrEqualTo(rightLimit); // considers all events to the left of rightLimit
                ScottPlot.Bar myBar = new ScottPlot.Bar() { Position = barCenter, FillColor = Colors.Azure, Value = historicLikelihoodOfPercentageDrop };
                myBar.Label = myBar.Value.Round(2).ToString();
                lock(bars)
                {
                    bars.Add(myBar);
                }
            }

            var barPlot = myPlot.Add.Bars(bars.ToArray());
            barPlot.ValueLabelStyle.FontSize = 10;

            myPlot.Axes.Bottom.Label.Text = "Minimum Sustained Value [%] with regards to initial Asset Value over " + (int)TimePeriod + " Month Period";
            myPlot.Axes.Left.Label.Text = "Likelihood of the Asset never depreciating below that value [%]";
            myPlot.SavePng(SaveLocationsConfiguration.GetMaxLossAnalysisSaveFileLocation(Symbol, this), numberOfBars * 30, 600);
        }

        /// <summary>
        /// Draws the historic overperformance of open-end LONG/CALL knockout certificates of specific leverages.
        /// Considers the barrier being adjusted daily.
        /// </summary>
        private void DrawLeveragedOverperformanceGraph()
        {
            ScottPlot.Plot myPlot = new();

            double stepSize = 0.1; // do not set this below 0.1. If you do, adjust the barX = Math.Round... bit below.
            double minLeverage = 1.1;
            double maxLeverage = 4.0;
            int numberOfBars = (int) double.Round((maxLeverage - minLeverage) / stepSize);
            List<ScottPlot.Bar> bars = new();
            List<Tick> tickList = new();
            double annualizedNonLeveragedPerformance = 0;

            Parallel.ForEach(Partitioner.Create(0, numberOfBars + 1), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    double barX = i * stepSize + minLeverage; // barX is the leverage and the actual intended x value of that bar, NOT the position of the bar on the graph
                    barX = Math.Round(barX, 1);

                    double barPositionX = i;
                    LeveragedOverperformanceAnalysisResult result = GetHistoricLeveragedOverperformanceAnalysis(barX);
                    ScottPlot.Bar myBar = new ScottPlot.Bar() { Position = barPositionX, FillColor = Colors.Azure, Value = result.AverageAnnualizedOverPerformancePercent };

                    lock (tickList)
                    {
                        tickList.Add(new(barPositionX, barX.ToString()));
                    }

                    myBar.Label = myBar.Value.Round(2).ToString()
                        + "\n" + result.KnockoutLikelihoodPercent.Round(2).ToString()
                        + "\n" + result.KnockoutOrLossLikelihoodPercent.Round(2).ToString()
                        + "\n" + result.LeveragedAvgAnnualizedPerformancePercentage.Round(2).ToString();

                    annualizedNonLeveragedPerformance = result.NonLeveragedAvgAnnualizedPerformancePercentage;
                    myBar.LabelOffset = 36f;
                    myBar.FillColor = Colors.Gray.WithAlpha(0.6);
                    myBar.BorderColor = Colors.Black.WithAlpha(0.7);

                    lock (bars)
                    {
                        bars.Add(myBar);
                    }

                    var koChanceLine = myPlot.Add.Line(barPositionX, 0, barPositionX, result.KnockoutLikelihoodPercent); // should this be locked or not?
                    koChanceLine.LineColor = Colors.Red.WithAlpha(0.65);
                    koChanceLine.LineWidth = 8.0f;

                    var koOrLossLine = myPlot.Add.Line(barPositionX + koChanceLine.LineWidth / 100.0, 0, barPositionX + koChanceLine.LineWidth / 100.0, result.KnockoutOrLossLikelihoodPercent);
                    koOrLossLine.LineColor = Colors.Orange.WithAlpha(0.65);
                    koOrLossLine.LineWidth = koChanceLine.LineWidth / 2.0f;
                }
            });

            myPlot.Title(Symbol.Overview.ToString() + " Leveraged Overperformance Analysis over " + (int)TimePeriod + " months." +
                " Barrier adjusted by " + AdjustmentPercentagePA + "% p.a." +
                " Average Annual Non-Leveraged Growth: " + annualizedNonLeveragedPerformance.Round(2) + "%");

            var barPlot = myPlot.Add.Bars(bars.ToArray());
            barPlot.ValueLabelStyle.FontSize = 12f;

            //https://scottplot.net/cookbook/5.0/CustomizingTicks/RotatedTicksLongLabels/
            myPlot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(tickList.ToArray());
            myPlot.Axes.Bottom.TickLabelStyle.Rotation = 45;
            myPlot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleLeft;

            //https://scottplot.net/cookbook/5.0/Annotation/AnnotationCustomize/
            var barAnnotation = myPlot.Add.Annotation(
                "1. Overperformance vs Underlying [%]" +
                "\n2. Likelihood of Knockout Event [%]" +
                "\n3. Likelihood of Knockout or Loss [%]" +
                "\n4. Annualized " + (int)TimePeriod + "-month Performance [%] ");
            barAnnotation.Label.FontSize = 15;
            barAnnotation.Label.BackColor = Colors.Blue.WithAlpha(.3);
            barAnnotation.Label.ForeColor = Colors.Black.WithAlpha(0.9);
            barAnnotation.Label.BorderColor = Colors.Blue.WithAlpha(0.5);
            barAnnotation.Label.BorderWidth = 1;

            myPlot.Axes.Bottom.Label.Text = "Leverage";
            myPlot.Axes.Left.Label.Text = "Overperformance vs Underlying Asset [%]";
            myPlot.SavePng(SaveLocationsConfiguration.GetLeveragedOverperformanceAnalysisSaveFileLocation(Symbol, this), numberOfBars * 50, 800);
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
            myPlot.SavePng(SaveLocationsConfiguration.GetGrowthAnalysisSaveFileLocation(Symbol, this), numberOfBars * 45, 500);
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
using MathNet.Numerics;
using Qlarissa.Chart;
using Qlarissa.Chart.Analysis.InverseLogRegression;
using Qlarissa.CustomConfiguration;
using ScottPlot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Qlarissa.ErrorCorrection
{
    public class ErrorCorrectionProfileForINVLOGRegression
    {
        public ErrorCorrectionProfileForINVLOGRegression(Symbol symbol)
        {
            //Stopwatch clock = Stopwatch.StartNew();
            Symbol = symbol;
            Frames = new();

            double minTrainingPeriodYears = 5.0;
            double maxTrainingPeriodYears = 13.5;

            double minLookAheadDurationYears = 1.0; // the data point for which a prediction is made is at least 1 year after the training period
            double maxLookAheadDurationYears = 1.0;
            double lookAheadStepSize = 0.025;
            double trainingPeriodIncrement = 0.02;

            double rightMostPredictionPeriodOffset = maxTrainingPeriodYears + maxLookAheadDurationYears;

            SymbolDataPoint[] dataPointsForAnalysis = symbol.GetDataPointsForAnalysis();

            double firstDataPointDate = dataPointsForAnalysis[0].Date.ToDouble();
            if (firstDataPointDate + rightMostPredictionPeriodOffset > dataPointsForAnalysis.Last().Date.ToDouble())
            {
                throw new ArgumentException("Not enough data for Symbol: " + symbol);
            }

            List<double> trainingPeriodEndOffsets = new();

            for (double trainingPeriodEndOffset = minTrainingPeriodYears; trainingPeriodEndOffset <= maxTrainingPeriodYears; trainingPeriodEndOffset += trainingPeriodIncrement)
            {
                trainingPeriodEndOffsets.Add(trainingPeriodEndOffset.Round(6));
            }

            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 5
            };

            Parallel.ForEach(trainingPeriodEndOffsets, parallelOptions, trainingPeriodEndOffset =>
            {
                SymbolDataPoint[] trainingPeriodDataPoints = dataPointsForAnalysis.Where(x => x.Date.ToDouble() <= firstDataPointDate + trainingPeriodEndOffset).ToArray();
                InverseLogRegressionResult model = new(trainingPeriodDataPoints);
                for (double currentLookAheadDurationYears = minLookAheadDurationYears; currentLookAheadDurationYears <= maxLookAheadDurationYears; currentLookAheadDurationYears += lookAheadStepSize)
                {
                    double lookAheadDataPointTargetDate = trainingPeriodDataPoints.Last().Date.ToDouble() + currentLookAheadDurationYears;
                    SymbolDataPoint lookAheadDataPoint = symbol.DataPoints.First(x => x.Date.ToDouble() >= lookAheadDataPointTargetDate);
                    if (!symbol.IsDateValidTargetDateForErrorAnalysis(lookAheadDataPoint.Date))
                    {
                        continue;
                    }

                    SymbolDataPoint expected = new() { Date = lookAheadDataPoint.Date, MediumPrice = model.GetEstimate(lookAheadDataPoint.Date.ToDouble()) };

                    ErrorCorrectionForInvLogFrame Frame = new(inverseLogModel: model,
                        expected: expected,
                        actual: lookAheadDataPoint,
                        lastDataPointInTrainingPeriod: trainingPeriodDataPoints.Last());

                    lock(Frames)
                    {
                        Frames.Add(Frame);
                    }
                }
            });

            /*for (double trainingPeriodEndOffset = minTrainingPeriodYears; trainingPeriodEndOffset <= maxTrainingPeriodYears; trainingPeriodEndOffset += trainingPeriodIncrement)
            {
                SymbolDataPoint[] trainingPeriodDataPoints = dataPointsForAnalysis.Where(x => x.Date.ToDouble() <= firstDataPointDate + trainingPeriodEndOffset).ToArray();
                InverseLogRegressionResult model = new(trainingPeriodDataPoints);
                for(double currentLookAheadDurationYears = minLookAheadDurationYears; currentLookAheadDurationYears <= maxLookAheadDurationYears; currentLookAheadDurationYears += lookAheadStepSize)
                {
                    double lookAheadDataPointTargetDate = trainingPeriodDataPoints.Last().Date.ToDouble() + currentLookAheadDurationYears;
                    SymbolDataPoint lookAheadDataPoint = symbol.DataPoints.First(x => x.Date.ToDouble() >= lookAheadDataPointTargetDate);
                    if (!symbol.IsDateValidTargetDateForErrorAnalysis(lookAheadDataPoint.Date))
                    {
                        //Console.WriteLine(lookAheadDataPoint.Date + " is an excluded target date");
                        continue;
                    }

                    SymbolDataPoint expected = new() { Date = lookAheadDataPoint.Date, MediumPrice = model.GetEstimate(lookAheadDataPoint.Date.ToDouble()) };

                    ErrorCorrectionForInvLogFrame Frame = new(inverseLogModel: model,
                        expected: expected,
                        actual: lookAheadDataPoint,
                        lastDataPointInTrainingPeriod: trainingPeriodDataPoints.Last());
                    Frames.Add(Frame);
                }
            }*/

            //clock.Stop();
            //Console.WriteLine(clock.Elapsed.TotalSeconds);
            /* max concurrency (using physical cores) | exec time [s]
             * Data for:
             * double minTrainingPeriodYears = 5.0;
            double maxTrainingPeriodYears = 13.5;

            double minLookAheadDurationYears = 1.0;
            double maxLookAheadDurationYears = 1.0;
            double lookAheadStepSize = 0.025;
            double trainingPeriodIncrement = 0.02;
             * 1 | 119
             * 2 | 90
             * 3 | 77
             * 4 | 76.5
             * 5 | 74
             * 6 | 73
             * 7 | 77 (evidently MaxDegreeOfParallelism refers to physical and not logical cores)
             * Default | 92
             * */
            DrawAllFrames();
            Draw_ExpBaseReg_Frames();
            Draw_LinBaseReg_Frames();
            Draw_LogBaseReg_Frames();
        }

        Symbol Symbol { get; set; }

        public List<ErrorCorrectionForInvLogFrame> Frames { get; private set; }

        public void DrawAllFrames()
        {
            ScottPlot.Plot myPlot = new();
            myPlot.Title(Symbol.Overview.ToString() +
                "\nCombined Error Heatmap by R² and Slope at the end of training period." +
                "\nBlue = Overestimation. Red = Underestimation. Green = Good Estimate.");

            myPlot.Axes.Title.Label.OffsetY = -40;

            foreach (var frame in Frames)
            {
                InvLogFrameAsHeatmapPoint heatmapPoint = new(frame);
                myPlot.Add.Marker(x: heatmapPoint.X_RSquared, y: heatmapPoint.Y_Slope, size: heatmapPoint.MarkerSize, color: heatmapPoint.Z_Color);
            }

            myPlot.Axes.Bottom.Label.Text = "R²";
            myPlot.Axes.Left.Label.Text = "Slope";
            myPlot.SavePng(SaveLocationsConfiguration.GetErrorAnalysisCombinedHeatmapSaveFileLocation(Symbol), 600, 500);
        }

        public void Draw_ExpBaseReg_Frames()
        {
            ScottPlot.Plot myPlot = new();
            myPlot.Title(Symbol.Overview.ToString() +
                "\nExp-Reg Error Heatmap by R² and Slope at the end of training period." +
                "\nBlue = Overestimation. Red = Underestimation. Green = Good Estimate.");

            myPlot.Axes.Title.Label.OffsetY = -40;

            IEnumerable<ErrorCorrectionForInvLogFrame> expBaseRegFrames = Frames.Where(x => x.InnerRegressionType == Chart.Enums.RegressionResultType.Exponential);

            foreach (var frame in expBaseRegFrames)
            {
                InvLogFrameAsHeatmapPoint heatmapPoint = new(frame);
                myPlot.Add.Marker(x: heatmapPoint.X_RSquared, y: heatmapPoint.Y_Slope, size: heatmapPoint.MarkerSize, color: heatmapPoint.Z_Color);
            }

            myPlot.Axes.Bottom.Label.Text = "R²";
            myPlot.Axes.Left.Label.Text = "Slope";
            myPlot.SavePng(SaveLocationsConfiguration.GetErrorAnalysis_ExpBaseRegression_HeatmapSaveFileLocation(Symbol), 700, 655);
        }

        public void Draw_LinBaseReg_Frames()
        {
            ScottPlot.Plot myPlot = new();
            myPlot.Title(Symbol.Overview.ToString() +
                "\nLin-Reg Error Heatmap by R² and Slope at the end of training period." +
                "\nBlue = Overestimation. Red = Underestimation. Green = Good Estimate.");

            myPlot.Axes.Title.Label.OffsetY = -40;

            IEnumerable<ErrorCorrectionForInvLogFrame> linBaseRegFrames = Frames.Where(x => x.InnerRegressionType == Chart.Enums.RegressionResultType.Linear);

            foreach (var frame in linBaseRegFrames)
            {
                InvLogFrameAsHeatmapPoint heatmapPoint = new(frame);
                myPlot.Add.Marker(x: heatmapPoint.X_RSquared, y: heatmapPoint.Y_Slope, size: heatmapPoint.MarkerSize, color: heatmapPoint.Z_Color);
            }

            myPlot.Axes.Bottom.Label.Text = "R²";
            myPlot.Axes.Left.Label.Text = "Slope";
            myPlot.SavePng(SaveLocationsConfiguration.GetErrorAnalysis_LinBaseRegression_HeatmapSaveFileLocation(Symbol), 700, 650);
        }

        public void Draw_LogBaseReg_Frames()
        {
            ScottPlot.Plot myPlot = new();
            myPlot.Title(Symbol.Overview.ToString() +
                "\nLog-Reg Error Heatmap by R² and Slope at the end of training period." +
                "\nBlue = Overestimation. Red = Underestimation. Green = Good Estimate.");

            myPlot.Axes.Title.Label.OffsetY = -40;

            IEnumerable<ErrorCorrectionForInvLogFrame> logBaseRegFrames = Frames.Where(x => x.InnerRegressionType == Chart.Enums.RegressionResultType.Logistic);

            foreach (var frame in logBaseRegFrames)
            {
                InvLogFrameAsHeatmapPoint heatmapPoint = new(frame);
                myPlot.Add.Marker(x: heatmapPoint.X_RSquared, y: heatmapPoint.Y_Slope, size: heatmapPoint.MarkerSize, color: heatmapPoint.Z_Color);
            }

            myPlot.Axes.Bottom.Label.Text = "R²";
            myPlot.Axes.Left.Label.Text = "Slope";
            myPlot.SavePng(SaveLocationsConfiguration.GetErrorAnalysis_LogBaseRegression_HeatmapSaveFileLocation(Symbol), 700, 650);
        }
    }
}
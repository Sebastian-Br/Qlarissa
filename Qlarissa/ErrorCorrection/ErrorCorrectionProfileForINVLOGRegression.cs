using MathNet.Numerics;
using Qlarissa.Chart;
using Qlarissa.Chart.Analysis.InverseLogRegression;
using Qlarissa.CustomConfiguration;
using ScottPlot;
using System;
using System.Collections.Generic;
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
            Symbol = symbol;
            Frames = new();

            double minTrainingPeriodYears = 5.0;
            double maxTrainingPeriodYears = 10.0;

            double minLookAheadDurationYears = 1.0; // the data point for which a prediction is made is at least 1 year after the training period
            double maxLookAheadDurationYears = 1.0;
            double lookAheadStepSize = 0.1;

            double rightMostPredictionPeriodOffset = maxTrainingPeriodYears + maxLookAheadDurationYears;
            if (symbol.DataPoints[0].Date.ToDouble() + rightMostPredictionPeriodOffset > symbol.DataPoints.Last().Date.ToDouble())
            {
                throw new ArgumentException("Not enough data for Symbol: " + symbol);
            }

            for (double trainingPeriodEndOffset = minTrainingPeriodYears; trainingPeriodEndOffset <= maxTrainingPeriodYears; trainingPeriodEndOffset += lookAheadStepSize)
            {
                SymbolDataPoint[] trainingPeriodDataPoints = symbol.DataPoints.Where(x => x.Date.ToDouble() <= trainingPeriodEndOffset).ToArray();
                InverseLogRegressionResult model = new(trainingPeriodDataPoints);
                for(double currentLookAheadDurationYears = minLookAheadDurationYears; currentLookAheadDurationYears <= maxLookAheadDurationYears; currentLookAheadDurationYears += lookAheadStepSize)
                {
                    double lookAheadDataPointTargetDate = trainingPeriodDataPoints.Last().Date.ToDouble() + currentLookAheadDurationYears;
                    SymbolDataPoint lookAheadDataPoint = symbol.DataPoints.First(x => x.Date.ToDouble() >= lookAheadDataPointTargetDate);
                    if (!symbol.IsDateValidTargetDateForErrorAnalysis(lookAheadDataPoint.Date))
                    {
                        Console.WriteLine(lookAheadDataPoint.Date + " is an excluded target date");
                        continue;
                    }

                    SymbolDataPoint expected = new() { Date = lookAheadDataPoint.Date, MediumPrice = model.GetEstimate(lookAheadDataPoint.Date.ToDouble()) };

                    ErrorCorrectionForInvLogFrame Frame = new(inverseLogModel: model,
                        expected: expected,
                        actual: lookAheadDataPoint,
                        lastDataPointInTrainingPeriod: trainingPeriodDataPoints.Last());
                    Frames.Add(Frame);
                }
            }

        }

        Symbol Symbol { get; set; }

        List<ErrorCorrectionForInvLogFrame> Frames { get; set; }

        public void DrawAllFrames()
        {
            ScottPlot.Plot myPlot = new();
            myPlot.Title(Symbol.ToString() + " Error Heatmap by R²and Slope at the end of training period. Blue = Overestimation. Red = Underestimation.");

            foreach (var frame in Frames)
            {
                InvLogFrameAsHeatmapPoint heatmapPoint = new(frame);
                myPlot.Add.Marker(x: heatmapPoint.X_RSquared, y: heatmapPoint.Y_Slope, size: heatmapPoint.MarkerSize, color: heatmapPoint.Z_Color);
            }

            myPlot.Axes.Bottom.Label.Text = "R²";
            myPlot.Axes.Left.Label.Text = "Slope";
            myPlot.SavePng(SaveLocationsConfiguration.GetErrorAnalysisHeatmapSaveFileLocation(Symbol), 500, 350);
        }
    }
}
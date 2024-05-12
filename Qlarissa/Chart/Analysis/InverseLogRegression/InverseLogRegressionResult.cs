using Qlarissa.Chart.Analysis.BaseRegressions;
using Qlarissa.Chart.Analysis.ExponentialRegression;
using Qlarissa.Chart.Enums;
using Qlarissa.CustomConfiguration;
using MathNet.Numerics;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qlarissa.Chart.Analysis.InverseLogRegression
{
    public class InverseLogRegressionResult : IRegressionResult
    {
        /// <summary>
        /// y(t) = e^(g(t)), where g is the best regression (log, linear, exp) for log(data)
        /// </summary>
        /// <param name="symbol"></param>
        public InverseLogRegressionResult(Symbol symbol)
        {
            SymbolDataPoint[] dataPoints = symbol.GetDataPointsForAnalysis();
            PreprocessingX0 = - 2000.0;
            double[] Xs = dataPoints.Select(dataPoint => dataPoint.Date.ToDouble()).ToArray();

            double[] preProcessedXs = dataPoints.Select(dataPoint => dataPoint.Date.ToDouble() + PreprocessingX0).ToArray();
            double[] logYs = dataPoints.Select(dataPoint => Math.Log(dataPoint.MediumPrice)).ToArray();

            InnerRegressions = new();
            LogisticRegressionResult = GetLogisticRegression_ExpWalk_ChatGPTed(preProcessedXs, logYs);
            InnerRegressions.Add(LogisticRegressionResult);

            LinearRegressionResultWithX0 linearRegression = new(preProcessedXs, logYs, PreprocessingX0);
            InnerRegressions.Add(linearRegression);

            ExponentialRegression.ExponentialRegression expReg = new ExponentialRegression.ExponentialRegression(Xs, logYs, -PreprocessingX0); // does preprocessing internally
            ExponentialRegression.ExponentialRegressionResult exponentialRegression = new(expReg, Xs, logYs);
            InnerRegressions.Add(exponentialRegression);

            InnerRegressions.Sort((a, b) => b.GetRsquared().CompareTo(a.GetRsquared())); // sorts regressions in descending order with respect to R²

            DrawWithLogReg(Xs, logYs, symbol);

            double[] Ys = dataPoints.Select(dataPoint => dataPoint.MediumPrice).ToArray();
            Rsquared = GoodnessOfFit.RSquared(Xs.Select(x => GetEstimate(x)), Ys);
            DateCreated = DateOnly.FromDateTime(DateTime.Now);
        }

        /// <summary>
        /// Used when analyzing a subset of datapoints
        /// </summary>
        /// <param name="dataPoints"></param>
        public InverseLogRegressionResult(SymbolDataPoint[] dataPoints)
        {
            PreprocessingX0 = -2000.0;
            double[] Xs = dataPoints.Select(dataPoint => dataPoint.Date.ToDouble()).ToArray();

            double[] preProcessedXs = dataPoints.Select(dataPoint => dataPoint.Date.ToDouble() + PreprocessingX0).ToArray();
            double[] logYs = dataPoints.Select(dataPoint => Math.Log(dataPoint.MediumPrice)).ToArray();

            InnerRegressions = new();
            LogisticRegressionResult = GetLogisticRegression_ExpWalk_ChatGPTed(preProcessedXs, logYs);
            InnerRegressions.Add(LogisticRegressionResult);

            LinearRegressionResultWithX0 linearRegression = new(preProcessedXs, logYs, PreprocessingX0);
            InnerRegressions.Add(linearRegression);

            ExponentialRegression.ExponentialRegression expReg = new ExponentialRegression.ExponentialRegression(Xs, logYs, -PreprocessingX0); // does preprocessing internally
            ExponentialRegression.ExponentialRegressionResult exponentialRegression = new(expReg, Xs, logYs);
            InnerRegressions.Add(exponentialRegression);

            InnerRegressions.Sort((a, b) => b.GetRsquared().CompareTo(a.GetRsquared())); // sorts regressions in descending order with respect to R²

            //DrawWithLogReg(Xs, logYs, symbol);

            double[] Ys = dataPoints.Select(dataPoint => dataPoint.MediumPrice).ToArray();
            Rsquared = GoodnessOfFit.RSquared(Xs.Select(x => GetEstimate(x)), Ys);
            DateCreated = DateOnly.FromDateTime(DateTime.Now);
        }

        DateOnly DateCreated {  get; set; }

        //List<double> Parameters { get; set; }

        double Rsquared { get; set; }

        double PreprocessingX0 { get; set; }

        RegressionResultType RegressionResult { get; set; } = RegressionResultType.InverseLogistic;

        LogisticRegressionResult LogisticRegressionResult { get; set; }

        List<IRegressionResult> InnerRegressions { get; set; }

        public DateOnly GetCreationDate()
        {
            return DateCreated;
        }

        public double GetEstimate(DateOnly date)
        {
            return GetEstimate(date.ToDouble());
        }

        public double GetEstimate(double t)
        {
            IRegressionResult bestRegression = InnerRegressions[0];
            if(bestRegression is LogisticRegressionResult)
            {
                t += PreprocessingX0;
            }

            return Math.Exp(bestRegression.GetEstimate(t));
        }

        public List<double> GetParameters()
        {
            return null;
        }

        public RegressionResultType GetRegressionResultType()
        {
            return RegressionResult;
        }

        public double GetRsquared()
        {
            return Rsquared;
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }

        public double GetWeight()
        {
            double weight = 1.0 / (1.0 - GetRsquared());
            return weight * weight;
        }

        private void DrawWithLogReg(double[] Xs, double[] Ys, Symbol symbol)
        {
            ScottPlot.Plot myPlot = new();
            Ys = Ys.Select(y => y).ToArray();
            var symbolScatter = myPlot.Add.Scatter(Xs, Ys);
            ScottPlot.Palettes.Category20 palette = new();
            symbolScatter.Color = palette.Colors[2];
            symbolScatter.LineWidth = 0.5f;

            myPlot.Title("Log of " + symbol.Overview.Symbol + " with Regressions");

            List<double> listXs = new();
            List<double> listLogRegYs = new();
            List<double> listLinRegYs = new();
            List<double> listExpRegYs = new();

            LogisticRegressionResult logisticRegression = (LogisticRegressionResult)InnerRegressions.Find(regression => regression.GetRegressionResultType() == RegressionResultType.Logistic);
            LinearRegressionResultWithX0 linearRegression = (LinearRegressionResultWithX0)InnerRegressions.Find(regression => regression.GetRegressionResultType() == RegressionResultType.Linear);
            ExponentialRegressionResult exponentialRegression = (ExponentialRegressionResult)InnerRegressions.Find(regression => regression.GetRegressionResultType() == RegressionResultType.Exponential);

            for (double d = Xs.First(); d <= Xs.Last(); d += 0.01)
            {
                listXs.Add(d);
                listLogRegYs.Add(logisticRegression.GetEstimate(d + PreprocessingX0)); // logistic regression doesn't store the pre-processing x0
                listLinRegYs.Add(linearRegression.GetEstimate(d));
                listExpRegYs.Add(exponentialRegression.GetEstimate(d));
            }

            double[] graphXs = listXs.ToArray();
            double[] LogRegYs = listLogRegYs.ToArray();
            var logScatter = myPlot.Add.Scatter(graphXs, LogRegYs);
            logScatter.Color = Colors.Green;
            logScatter.MarkerSize = 1.0f;
            logScatter.Label = "Logistic Regression";

            double[] LinRegYs = listLinRegYs.ToArray();
            var linScatter = myPlot.Add.Scatter(graphXs, LinRegYs);
            linScatter.Color = Colors.Blue;
            linScatter.MarkerSize = 1.0f;
            linScatter.Label = "Linear Regression";

            double[] ExpRegYs = listExpRegYs.ToArray();
            var expScatter = myPlot.Add.Scatter(graphXs, ExpRegYs);
            expScatter.Color = Colors.Red;
            expScatter.MarkerSize = 1.0f;
            expScatter.Label = "Exponential Regression";

            //https://scottplot.net/cookbook/5.0/Annotation/AnnotationCustomize/
            var logRegAnnotation = myPlot.Add.Annotation("LogRegR²=" + logisticRegression.GetRsquared());
            logRegAnnotation.Label.FontSize = 18;
            logRegAnnotation.Label.BackColor = Colors.Gray.WithAlpha(.3);
            logRegAnnotation.Label.ForeColor = Colors.Black.WithAlpha(0.8);
            logRegAnnotation.Label.BorderColor = Colors.Gray.WithAlpha(0.5);
            logRegAnnotation.Label.BorderWidth = 1;

            var linRegAnnotation = myPlot.Add.Annotation("LinRegR²=" + linearRegression.GetRsquared());
            linRegAnnotation.Label.FontSize = 18;
            linRegAnnotation.Label.BackColor = Colors.Gray.WithAlpha(.3);
            linRegAnnotation.Label.ForeColor = Colors.Black.WithAlpha(0.8);
            linRegAnnotation.Label.BorderColor = Colors.Gray.WithAlpha(0.5);
            linRegAnnotation.Label.BorderWidth = 1;
            linRegAnnotation.OffsetY = 35;

            var expRegAnnotation = myPlot.Add.Annotation("ExpRegR²=" + exponentialRegression.GetRsquared());
            expRegAnnotation.Label.FontSize = 18;
            expRegAnnotation.Label.BackColor = Colors.Gray.WithAlpha(.3);
            expRegAnnotation.Label.ForeColor = Colors.Black.WithAlpha(0.8);
            expRegAnnotation.Label.BorderColor = Colors.Gray.WithAlpha(0.5);
            expRegAnnotation.Label.BorderWidth = 1;
            expRegAnnotation.OffsetY = 70;


            myPlot.Legend.Show();
            myPlot.SavePng(SaveLocationsConfiguration.GetLogRegressionsSaveFileLocation(symbol), 1100, 600);
        }

        private LogisticRegressionResult GetLogisticRegression_ExpWalk_ChatGPTed(double[] Xs, double[] Ys)
        {
            double min_xDelta0 = -0.001;
            double xDelta0 = min_xDelta0;
            double firstDateIndex = Xs.First();
            double currentX0 = firstDateIndex - xDelta0;

            int maxIterations = 200000;
            int iteration = 0;

            double currentBest_rSquared = 0;
            double stepSize = -0.001;
            double exitStepSize = 1e-308; // Exit condition based on step size
            double lastValid_xDelta0 = xDelta0;
            double bestA = 0, bestB = 0, bestRSquared = 0;

            while (iteration < maxIterations && Math.Abs(stepSize) >= exitStepSize)
            {
                currentX0 = firstDateIndex + xDelta0;

                double[] x = Xs.Select(xx => (xx - currentX0)).ToArray();
                double[] y = Ys;
                var p = Fit.Logarithm(x, y);
                double a = p.Item1;
                double b = p.Item2;
                double rSquared = GoodnessOfFit.RSquared(x.Select(x => a + b * Math.Log(x)), y);

                if (rSquared > currentBest_rSquared)
                {
                    // Update current best result
                    currentBest_rSquared = rSquared;
                    bestA = a;
                    bestB = b;
                    bestRSquared = rSquared;

                    // Adjust step size for the next iteration using an exponential-walk approach
                    stepSize *= 1.1; // Increase step size exponentially
                    lastValid_xDelta0 = xDelta0;
                }
                else
                {
                    // Overshoot occurred, restore previous best result and decrease step size
                    xDelta0 = lastValid_xDelta0;
                    stepSize *= 0.5; // Decrease step size

                    // Prevent step size from becoming too small
                    if (Math.Abs(stepSize) < exitStepSize)
                        break;
                }

                xDelta0 += stepSize;
                iteration++;
            }

            //Console.WriteLine("Finished Logistic Regression Exp Walk. Iterations: " + iteration);
            LogisticRegressionResult result = new LogisticRegressionResult(bestRSquared, A: bestB, B: bestA, _x0: firstDateIndex + lastValid_xDelta0, Xs.First());
            return result;
        }

        public IRegressionResult GetEffectiveInnerRegression()
        {
            return InnerRegressions[0];
        }
    }
}
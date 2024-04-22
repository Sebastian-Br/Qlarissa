﻿using Charty.Chart.Analysis.BaseRegressions;
using Charty.Chart.Enums;
using MathNet.Numerics;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.Analysis.InverseLogRegression
{
    public class InverseLogRegressionResult : IRegressionResult
    {
        /// <summary>
        /// y(t) = e^(p0 * Math.Log(t - X0) + p1)
        /// </summary>
        /// <param name="symbol"></param>
        public InverseLogRegressionResult(Symbol symbol)
        {
            SymbolDataPoint[] dataPoints = symbol.GetDataPointsNotInExcludedTimePeriods();
            double[] Xs = dataPoints.Select(dataPoint => dataPoint.Date.ToDouble()).ToArray();
            double[] logYs = dataPoints.Select(dataPoint => Math.Log(dataPoint.MediumPrice)).ToArray();

            LogisticRegressionResult = GetLogisticRegression_ExpWalk_ChatGPTed(Xs, logYs);
            DrawWithLogReg(Xs, logYs, LogisticRegressionResult, symbol);

            double[] Ys = dataPoints.Select(dataPoint => dataPoint.MediumPrice).ToArray();
            Rsquared = GoodnessOfFit.RSquared(Xs.Select(x => GetEstimate(x)), Ys);
            DateCreated = DateOnly.FromDateTime(DateTime.Now);
        }

        DateOnly DateCreated {  get; set; }

        //List<double> Parameters { get; set; }

        double Rsquared { get; set; }

        RegressionResultType RegressionResult { get; set; } = RegressionResultType.InverseLogistic;

        LogisticRegressionResult LogisticRegressionResult { get; set; }

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
            return Math.Exp(LogisticRegressionResult.GetEstimate(t));
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
            // y(t) = e^(p0 * Math.Log(t - X0) + p1)
            return "y(t) = e^(" + LogisticRegressionResult.GetParameters()[0] + " * ln(t - " + LogisticRegressionResult.X0 + ") + " + LogisticRegressionResult.GetParameters()[1] + ")" 
                + "[R²=" + Rsquared + "]";
        }

        public double GetWeight()
        {
            double weight = 1.0 / (1.0 - GetRsquared());
            return weight * weight;
        }

        private void DrawWithLogReg(double[] Xs, double[] Ys, LogisticRegressionResult l, Symbol symbol)
        {
            ScottPlot.Plot myPlot = new();
            Ys = Ys.Select(y => y).ToArray();
            var symbolScatter = myPlot.Add.Scatter(Xs, Ys);
            ScottPlot.Palettes.Category20 palette = new();
            symbolScatter.Color = palette.Colors[2];
            symbolScatter.LineWidth = 0.5f;

            myPlot.Title("LogReg of + " + symbol.Overview.Symbol + " with Regression [R²=" + l.GetRsquared() + "]");

            List<double> listLogRegXs = new();
            List<double> listLogRegYs = new();

            for(double d = Xs.First(); d <= Xs.Last(); d += 0.01)
            {
                listLogRegXs.Add(d);
                listLogRegYs.Add(l.GetEstimate(d));
            }

            double[] LogRegXs = listLogRegXs.ToArray();
            double[] LogRegYs = listLogRegYs.ToArray();
            LogRegYs = LogRegYs.Select(y2 => y2).ToArray();
            var logScatter = myPlot.Add.Scatter(LogRegXs, LogRegYs);
            logScatter.Color = Colors.Green;
            logScatter.LineWidth = 0.4f;

            myPlot.SavePng(symbol.Overview.Symbol + "_InvLog_WithRegression.png", 900, 600);
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
    }
}
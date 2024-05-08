using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using MathNet.Numerics;
using MathNet.Numerics.Optimization;
using ScottPlot;
using System.Collections.Concurrent;

namespace Qlarissa.Chart.Analysis.ExponentialRegression
{
    /// <summary>
    /// y = a * b^x
    /// </summary>
    public class ExponentialRegression
    {
        public ExponentialRegression(Symbol symbol, double initialA = 1.0, double initialB = 1.0, double initialX0 = 2000.0)
        {
            SymbolDataPoint[] dataPoints = symbol.GetDataPointsNotInExcludedTimePeriods();

            if (dataPoints == null || dataPoints.Length == 0)
                throw new ArgumentException("No Data Points");

            double[] x = new double[dataPoints.Length]; // could skip this new array initialization
            double[] y = new double[dataPoints.Length];
            for (int i = 0; i < dataPoints.Length; i++)
            {
                //Console.WriteLine(chartDataPoints[i].Date);
                x[i] = dataPoints[i].Date.ToDouble();
                y[i] = dataPoints[i].MediumPrice;
            }

            X0 = initialX0;

            // Define the exponential model function                                    p = vector of parameters (a,b)
            Func<double, MathNet.Numerics.LinearAlgebra.Vector<double>, double> model = (t, p) => p[0] * Math.Pow(p[1], t - X0);

            // Define the objective function to minimize the residual sum of squares
            Func<MathNet.Numerics.LinearAlgebra.Vector<double>, double> objective = p =>
            {
                double sumOfSquares = 0;
                double[] squares = new double[x.Length];

                Parallel.ForEach(Partitioner.Create(0, x.Length), range => // reduces execution times by around 55%
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        double residual = model(x[i], p) - y[i];
                        double residualSquare = residual * residual;
                        squares[i] = 1e-16 * residualSquare; // you have to use a small factor to avoid issues when adding all of the values
                    }
                });

                sumOfSquares = squares.Sum();
                return sumOfSquares;
            };

            var objFunction = ObjectiveFunction.Value(objective);

            MathNet.Numerics.LinearAlgebra.Vector<double> initialParameterGuesses = 
                MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfArray(new[] { initialA, initialB });

            // Use Levenberg-Marquardt algorithm to minimize the objective function
            NelderMeadSimplex nms = new(1e-14, 600000);
            var result = nms.FindMinimum(objFunction, initialParameterGuesses);

            // Extract optimized parameters
            A = result.MinimizingPoint[0]; // 2.7252
            B = result.MinimizingPoint[1]; // 1.3324
            //Console.WriteLine("ExponentialRegression: y = " + A + " * " + B + " ^x" + " // after " + result.Iterations + " iterations");
        }

        public ExponentialRegression(double[] x, double[] y, double x0, double initialA = 1.0, double initialB = 1.0)
        {
            X0 = x0;
            // Define the exponential model function                                    p = vector of parameters (a,b)
            Func<double, MathNet.Numerics.LinearAlgebra.Vector<double>, double> model = (t, p) => p[0] * Math.Pow(p[1], t - X0);

            // Define the objective function to minimize the residual sum of squares
            Func<MathNet.Numerics.LinearAlgebra.Vector<double>, double> objective = p =>
            {
                double sumOfSquares = 0;
                double[] squares = new double[x.Length];

                Parallel.ForEach(Partitioner.Create(0, x.Length), range => // reduces execution times by around 55%
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        double residual = model(x[i], p) - y[i];
                        squares[i] = 1e-16 * residual * residual; // you have to use a small factor to avoid issues when adding all of the values
                    }
                });

                sumOfSquares = squares.Sum();
                return sumOfSquares;
            };

            var objFunction = ObjectiveFunction.Value(objective);

            MathNet.Numerics.LinearAlgebra.Vector<double> initialParameterGuesses =
                MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfArray(new[] { initialA, initialB });

            // Use Levenberg-Marquardt algorithm to minimize the objective function
            NelderMeadSimplex nms = new(1e-15, 600000);
            var result = nms.FindMinimum(objFunction, initialParameterGuesses);

            // Extract optimized parameters
            A = result.MinimizingPoint[0]; // 2.7252
            B = result.MinimizingPoint[1]; // 1.3324
            //Console.WriteLine("ExponentialRegression: y = " + A + " * " + B + " ^x" + " // after " + result.Iterations + " iterations");
        }

        public double A { get; private set; }

        public double B { get; private set; }

        public double X0 { get; private set; }
    }
}
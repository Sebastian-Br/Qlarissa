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
using Charty.Chart.Analysis.CascadingCAGR;

namespace Charty.Chart.Analysis.ExponentialRegression
{
    /// <summary>
    /// y = a * b^x
    /// </summary>
    public class ExponentialRegression
    {
        public ExponentialRegression(SymbolDataPoint[] dataPoints, double initialA = 1.0, double initialB = 1.0)
        {
            if (dataPoints == null || dataPoints.Length == 0)
                throw new ArgumentException("No Data Points");

            DataPoints = dataPoints;
            double[] x = new double[dataPoints.Length]; // could skip this new array initialization
            double[] y = new double[dataPoints.Length];
            for (int i = 0; i < dataPoints.Length; i++)
            {
                //Console.WriteLine(chartDataPoints[i].Date);
                x[i] = GetYearIndex(dataPoints[i]);
                y[i] = dataPoints[i].MediumPrice;
            }

            // Define the exponential model function                                    p = vector of parameters (a,b)
            Func<double, MathNet.Numerics.LinearAlgebra.Vector<double>, double> model = (t, p) => p[0] * Math.Pow(p[1], t);

            // Define the objective function to minimize the residual sum of squares
            Func<MathNet.Numerics.LinearAlgebra.Vector<double>, double> objective = p =>
            {
                double sum = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    double residual = model(x[i], p) - y[i];
                    sum += 0.01 * residual * residual;
                }
                return sum;
            };

            var objFunction = ObjectiveFunction.Value(objective);

            MathNet.Numerics.LinearAlgebra.Vector<double> initialGuess = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfArray(new[] { initialA, initialB });

            // Use Levenberg-Marquardt algorithm to minimize the objective function
            NelderMeadSimplex nms = new(1e-14, 1200000);
            var result = nms.FindMinimum(objFunction, initialGuess);

            // Extract optimized parameters
            A = result.MinimizingPoint[0];
            B = result.MinimizingPoint[1];
            CaculateRsquared();
            //Console.WriteLine("ExponentialRegression: y = " + A + " * " + B + " ^x" + " // after " + result.Iterations + " iterations");
        }

        public double A { get; private set; }

        public double B { get; private set; }

        public SymbolDataPoint[] DataPoints { get; private set; } 

        public double Rsquared { get; private set; }

        private void CaculateRsquared()
        {
            double[] Xs = DataPoints.Select(x => x.Date.ToDouble()).ToArray();
            double[] Ys = DataPoints.Select(x => x.MediumPrice).ToArray();

            Rsquared = GoodnessOfFit.RSquared(Xs.Select(x => A * Math.Pow(B, x)), Ys);
        }

        private double GetYearIndex(SymbolDataPoint dataPoint)
        {
            return ConvertDateToYearIndex(dataPoint.Date);
        }

        private double ConvertDateToYearIndex(DateOnly date)
        {
            int year = date.Year;
            int dayOfYear = date.DayOfYear;
            int daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
            double yearIndex = year + dayOfYear / (double)daysInYear;
            return yearIndex;
        }
    }
}
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

namespace Charty.Chart.Analysis.ExponentialRegression
{
    /// <summary>
    /// y = a * b^x
    /// </summary>
    public class ExponentialRegression
    {
        public ExponentialRegression(SymbolDataPoint[] chartDataPoints)
        {
            if (chartDataPoints == null || chartDataPoints.Length == 0)
                throw new ArgumentException("No Data Points");

            double[] x = new double[chartDataPoints.Length]; // could skip this new array initialization
            double[] y = new double[chartDataPoints.Length];
            for (int i = 0; i < chartDataPoints.Length; i++)
            {
                //Console.WriteLine(chartDataPoints[i].Date);
                x[i] = GetYearIndex(chartDataPoints[i]);
                y[i] = chartDataPoints[i].MediumPrice;
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
                    sum += residual * residual;
                }
                return sum;
            };

            var objFunction = ObjectiveFunction.Value(objective);

            double initialA = 1.0;
            double initialB = 1.0;
            MathNet.Numerics.LinearAlgebra.Vector<double> initialGuess = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfArray(new[] { initialA, initialB });

            // Use Levenberg-Marquardt algorithm to minimize the objective function
            NelderMeadSimplex nms = new(1e-14, 1000000);
            var result = nms.FindMinimum(objFunction, initialGuess);

            // Extract optimized parameters
            A = result.MinimizingPoint[0];
            B = result.MinimizingPoint[1];
            Console.WriteLine("ExponentialRegression: y = " + A + " * " + B + " ^x");
        }

        public double A { get; private set; }

        public double B { get; private set; }

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
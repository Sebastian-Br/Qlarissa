using Qlarissa.Chart.Enums;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ScottPlot.Generate;
using static System.Runtime.InteropServices.JavaScript.JSType;
using DateTime = System.DateTime;

namespace Qlarissa.Chart.Analysis.ExponentialRegression
{
    public class ExponentialRegressionResult : IRegressionResult
    {
        public ExponentialRegressionResult(ExponentialRegression e, Symbol symbol)
        {
            Parameters = new();
            Parameters.Add(e.A);
            Parameters.Add(e.B);
            Parameters.Add(e.X0);
            Overview = symbol.Overview;

            SymbolDataPoint[] dataPoints = symbol.GetDataPointsForAnalysis();
            double[] Xs = dataPoints.Select(x => x.Date.ToDouble()).ToArray();
            double[] Ys = dataPoints.Select(x => x.MediumPrice).ToArray();
            Rsquared = GoodnessOfFit.RSquared(Xs.Select(x => GetEstimate(x)), Ys);
            DateCreated = DateOnly.FromDateTime(DateTime.Today);
        }

        public ExponentialRegressionResult(ExponentialRegression e, double[] Xs, double[] Ys)
        {
            Parameters = new();
            Parameters.Add(e.A);
            Parameters.Add(e.B);
            Parameters.Add(e.X0);
            Overview = null;
            Rsquared = GoodnessOfFit.RSquared(Xs.Select(x => GetEstimate(x)), Ys);
            DateCreated = DateOnly.FromDateTime(DateTime.Today);
        }

        List<double> Parameters { get; set; }

        public SymbolOverview Overview { get; private set; }

        public RegressionResultType RegressionResult { get; private set; } = RegressionResultType.Exponential;

        double Rsquared { get; set; }

        public DateOnly DateCreated { get; private set; }

        public double GetEstimate(double t)
        {
            return Parameters[0] * Math.Pow(Parameters[1], t - Parameters[2]);
        }

        public double GetEstimate(DateOnly date)
        {
            return GetEstimate(date.ToDouble());
        }

        public override string ToString()
        {
            return "y(t) = " + Parameters[0] + " * " + Parameters[1] + " ^ (t - " + Parameters[2] + ") [R²=" + Rsquared + "]";
        }

        public List<double> GetParameters()
        {
            return Parameters;
        }

        public double GetRsquared()
        {
            return Rsquared;
        }

        public DateOnly GetCreationDate()
        {
            return DateCreated;
        }

        public RegressionResultType GetRegressionResultType()
        {
            return RegressionResult;
        }

        public double GetWeight()
        {
            double weight = 1.0 / (1.0 - GetRsquared());
            return weight * weight;
        }
    }
}
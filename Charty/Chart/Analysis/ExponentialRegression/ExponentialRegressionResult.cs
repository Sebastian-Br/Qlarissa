using Charty.Chart.Enums;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ScottPlot.Generate;
using static System.Runtime.InteropServices.JavaScript.JSType;
using DateTime = System.DateTime;

namespace Charty.Chart.Analysis.ExponentialRegression
{
    public class ExponentialRegressionResult : IRegressionResult
    {
        public ExponentialRegressionResult(ExponentialRegression e, Symbol symbol)
        {
            A = e.A;
            B = e.B;
            X0 = e.X0;
            Overview = symbol.Overview;

            SymbolDataPoint[] dataPoints = symbol.GetDataPointsNotInExcludedTimePeriods();
            double[] Xs = dataPoints.Select(x => x.Date.ToDouble()).ToArray();
            double[] Ys = dataPoints.Select(x => x.MediumPrice).ToArray();
            Rsquared = GoodnessOfFit.RSquared(Xs.Select(x => GetEstimate(x)), Ys);
            DateCreated = DateOnly.FromDateTime(DateTime.Today);
        }

        public ExponentialRegressionResult(ExponentialRegression e, double[] Xs, double[] Ys)
        {
            A = e.A;
            B = e.B;
            X0 = e.X0;
            Overview = null;
            Rsquared = GoodnessOfFit.RSquared(Xs.Select(x => GetEstimate(x)), Ys);
            DateCreated = DateOnly.FromDateTime(DateTime.Today);
        }

        public double A { get; private set; }

        public double B { get; private set; }

        public double X0 { get; private set; }

        public SymbolOverview Overview { get; private set; }

        public RegressionResultType RegressionResult { get; private set; } = RegressionResultType.Exponential;

        double Rsquared { get; set; }

        public DateOnly DateCreated { get; private set; }

        public double GetEstimate(double t)
        {
            return A * Math.Pow(B, t - X0);
        }

        public double GetEstimate(DateOnly date)
        {
            return GetEstimate(date.ToDouble());
        }

        public override string ToString()
        {
            return "y(t) = " + A + " * " + B + " ^ (t - " + X0 + ") [R²=" + Rsquared + "]";
        }

        public List<double> GetParameters()
        {
            throw new NotImplementedException();
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
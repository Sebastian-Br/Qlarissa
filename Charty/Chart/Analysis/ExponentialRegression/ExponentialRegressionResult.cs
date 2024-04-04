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
        /// <summary>
        /// Used when importing data from the DB
        /// </summary>
        /// <param name="_A"></param>
        /// <param name="_B"></param>
        /// <param name="_Overview"></param>
        /// <param name="_CurrentPrice"></param>
        /// <param name="_OneYearGrowthEstimatePercentage"></param>
        /// <param name="_ThreeYearGrowthEstimatePercentage"></param>
        /// <param name="_DateCreated"></param>
        public ExponentialRegressionResult(double _A, double _B, SymbolOverview _Overview, double _CurrentPrice,
            DateOnly _DateCreated)
        {
            A = _A;
            B = _B;
            Overview = _Overview;
            CurrentPrice = _CurrentPrice;
            DateCreated = _DateCreated;
        }

        public ExponentialRegressionResult(ExponentialRegression e, Symbol symbol)
        {
            A = e.A;
            B = e.B;
            CurrentPrice = symbol.DataPoints.Last().MediumPrice;
            Overview = symbol.Overview;

            SymbolDataPoint[] dataPoints = symbol.GetDataPointsNotInExcludedTimePeriods();
            double[] Xs = dataPoints.Select(x => x.Date.ToDouble()).ToArray();
            double[] Ys = dataPoints.Select(x => x.MediumPrice).ToArray();
            Rsquared = GoodnessOfFit.RSquared(Xs.Select(x => GetEstimate(x)), Ys);
            DateCreated = DateOnly.FromDateTime(DateTime.Today);

            if (CurrentPrice <= 0)
            {
                throw new ArgumentException("currentPrice can not be less than or equal to 0");
            }
        }

        public double A { get; private set; }

        public double B { get; private set; }

        public SymbolOverview Overview { get; private set; }

        public double CurrentPrice { get; private set; }

        public RegressionResultType RegressionResult { get; private set; } = RegressionResultType.Exponential;

        double Rsquared { get; set; }

        public DateOnly DateCreated { get; private set; }

        public double GetEstimate(double t)
        {
            return A * Math.Pow(B, t);
        }

        public double GetEstimate(DateOnly date)
        {
            return GetEstimate(date.ToDouble());
        }

        public override string ToString()
        {
            return "y(t) = " + A + " * " + B + " ^ t + [R²=" + Rsquared + "]";
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
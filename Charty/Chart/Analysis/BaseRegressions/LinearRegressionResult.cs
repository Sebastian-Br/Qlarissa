using Charty.Chart.Enums;
using MathNet.Numerics.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.Analysis.BaseRegressions
{
    internal class LinearRegressionResult : IRegressionResult
    {
        /// <summary>
        /// y = m*t + c
        /// </summary>
        /// <param name="m">Slope</param>
        /// <param name="c">Intercept</param>
        public LinearRegressionResult(double rSquared, double m, double c)
        {
            Parameters = new();
            Parameters.Add(m);
            Parameters.Add(c);
            Rsquared = rSquared;
            DateCreated = DateOnly.FromDateTime(DateTime.Now);
        }
        List<double> Parameters { get; set; }

        double Rsquared { get; set; }

        RegressionResultType RegressionResult { get; set; } = RegressionResultType.Linear;

        DateOnly DateCreated { get; set; }

        public DateOnly GetCreationDate()
        {
            throw new NotImplementedException();
        }

        public double GetEstimate(double t)
        {
            return Parameters[0] * t + Parameters[1];
        }

        public double GetEstimate(DateOnly date)
        {
            double t = date.ToDouble();
            return GetEstimate(t);
        }

        public List<double> GetParameters()
        {
            return Parameters;
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
            return "y(t) = " + Parameters[0] + " * t + " + Parameters[1] + " [R²=" + Rsquared + "]"; ;
        }

        public double GetWeight()
        {
            double weight = 1.0 / (1.0 - GetRsquared());
            return weight * weight;
        }
    }
}
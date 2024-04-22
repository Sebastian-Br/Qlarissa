using Charty.Chart.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.Analysis.BaseRegressions
{
    internal class LogisticRegressionResult : IRegressionResult
    {
        /// <summary>
        /// a * ln(t - X0) + b
        /// </summary>
        public LogisticRegressionResult(double rSquared, double A, double B, double _x0, double constantT)
        {
            Parameters = new();
            Rsquared = rSquared;
            Parameters.Add(A);
            Parameters.Add(B);
            X0 = _x0;
            DateCreated = DateOnly.FromDateTime(DateTime.Now);
            ConstantT = constantT;
        }

        List<double> Parameters { get; set; }

        double Rsquared { get; set; }

        RegressionResultType RegressionResult { get; set; } = RegressionResultType.Logistic;

        public double ConstantT { get; private set; }

        DateOnly DateCreated { get; set; }

        public double GetEstimate(double t)
        {
            if (t < ConstantT) // ln(t) is only defined for t > 0
            {
                return Parameters[0] * Math.Log(ConstantT - X0) + Parameters[1];
            }

            return Parameters[0] * Math.Log(t - X0) + Parameters[1];
        }

        public double GetEstimate(DateOnly date)
        {
            double t = date.ToDouble();
            return GetEstimate(t);
        }

        public double X0 { get; private set; }

        public List<double> GetParameters()
        {
            return Parameters;
        }

        public double GetRsquared()
        {
            return Rsquared;
        }

        public override string ToString()
        {
            return "y(t) = " + Parameters[0] + " * ln(t - " + X0 + ") + " + Parameters[1] + " [R²=" + Rsquared + "]";
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
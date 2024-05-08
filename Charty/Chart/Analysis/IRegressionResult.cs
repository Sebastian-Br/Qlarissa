using Qlarissa.Chart.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qlarissa.Chart.Analysis
{
    public interface IRegressionResult
    {
        public List<double> GetParameters();

        public double GetRsquared();

        public double GetEstimate(DateOnly date);

        public double GetEstimate(double t);

        public DateOnly GetCreationDate();

        public RegressionResultType GetRegressionResultType();

        public double GetWeight();

        public string ToString();
    }
}
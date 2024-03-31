﻿using Charty.Chart.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.Analysis.CascadingCAGR
{
    internal interface IRegressionResult
    {
        public List<double> GetParameters();

        public double GetRsquared();

        public double GetEstimate(DateOnly date);

        public double GetEstimate(double t);

        public DateOnly GetCreationDate();

        public RegressionResult GetRegressionResultType();

        public string ToString();
    }
}
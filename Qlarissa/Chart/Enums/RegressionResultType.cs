using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qlarissa.Chart.Enums;

public enum RegressionResultType
{
    Linear = 1,
    Logistic = 2,
    Exponential = 3,
    Polynomial = 4,
    LinearWeighted = 5,
    ProjectingCAGR = 6,
    InverseLogistic = 7,
    LogisticallyCapped = 8,
}
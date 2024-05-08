using Qlarissa.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qlarissa.ErrorCorrection
{
    public class ErrorCorrectionProfileForINVLOGRegression
    {
        public ErrorCorrectionProfileForINVLOGRegression(Symbol symbol)
        {
            Symbol = symbol;
        }

        Symbol Symbol { get; set; }
    }
}
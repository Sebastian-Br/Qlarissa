using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.Api
{
    internal interface IApiManager
    {
        public Task<Symbol> RetrieveSymbol(string symbol);
    }
}
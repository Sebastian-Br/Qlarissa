using Charty.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Menu
{
    public interface IMenu
    {
        public string StateName();

        public string HelpMenu();

        public void PrintNameAndMenu();

        public Task<IMenu> SendText(string text);

        SymbolManager SymbolManager { get; set; }
    }
}
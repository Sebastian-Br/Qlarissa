using Charty.Chart.Api.PyFinance;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.Api.PYfinance
{
    internal class PyFiSymbol
    {
        public PyFiSymbol() { }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string Currency { get; set; }
        public long MarketCapitalization { get; set; }
        public double TrailingPE { get; set; }

        public double ForwardPE { get; set; }
        public double DividendPerShareYearly { get; set; }
        public Dictionary<string, PyFiDataPoint> HistoricalData { get; set; }

        public Symbol ToBusinessEntity()
        {
            SymbolOverview overview = new();
            overview.Symbol = Symbol;
            overview.Name = Name;
            overview.Currency = CurrencyExtensions.ToEnum(Currency);
            overview.DividendPerShareYearly = DividendPerShareYearly;
            overview.TrailingPE = TrailingPE;
            overview.ForwardPE = ForwardPE;
            overview.MarketCapitalization = MarketCapitalization;

            List <SymbolDataPoint> symbolDataPointList = new();

            foreach (KeyValuePair<string,PyFiDataPoint> entry in HistoricalData)
            {
                SymbolDataPoint symbolDataPoint = new();
                symbolDataPoint.Date = DateOnly.ParseExact(entry.Key, "yyyy-MM-dd", null);
                symbolDataPoint.LowPrice = entry.Value.Low;
                symbolDataPoint.HighPrice = entry.Value.High;
                symbolDataPoint.MediumPrice = (symbolDataPoint.LowPrice + symbolDataPoint.HighPrice) / 2.0;

                symbolDataPointList.Add(symbolDataPoint);
            }

            Symbol symbol = new(symbolDataPointList.ToArray(), overview);
            return symbol;
        }
    }
}
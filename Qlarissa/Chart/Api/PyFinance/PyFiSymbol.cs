using Qlarissa.Chart.Api.PyFinance;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qlarissa.Chart.Api.PYfinance;
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

    public string InvestorRelationsWebsite {  get; set; }

    public double TargetMeanPrice { get; set; }

    public int NumberOfAnalystOpinions { get; set; }

    public long SharesOutstanding { get; set; }

    public Dictionary<string, PyFiDataPoint> HistoricalData { get; set; }
    public Dictionary<string, IncomeStatement> RecentFourQuartersIncomeStatements { get; set; }

    public Dictionary<string, double> DividendHistory { get; set; }

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
            symbolDataPoint.MediumPrice = (entry.Value.Open + entry.Value.Close) / 2.0;

            symbolDataPointList.Add(symbolDataPoint);
        }

        Dictionary<DateOnly, double> dividendHistory = new();

        foreach (KeyValuePair<string, double> dividendPayout in DividendHistory)
        {
            DateOnly dividendPayoutDate = DateOnly.ParseExact(dividendPayout.Key, "yyyy-MM-dd", null);
            dividendHistory.Add(dividendPayoutDate, dividendPayout.Value);
        }

        Symbol symbol = new(symbolDataPointList.ToArray(), overview, dividendHistory);
        return symbol;
    }
}
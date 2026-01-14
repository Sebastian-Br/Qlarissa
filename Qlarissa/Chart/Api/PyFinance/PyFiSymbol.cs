using Qlarissa.Chart.Api.PyFinance;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

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

    public double ComputedRecommendationMean { get; set; }

    public int NumberOfAnalystOpinions { get; set; }

    //public long SharesOutstanding { get; set; }

    public Dictionary<string, PyFiDataPoint> HistoricalData { get; set; }

    //public Dictionary<string, IncomeStatement> RecentFourQuartersIncomeStatements { get; set; }

    public Dictionary<string, double> DividendHistory { get; set; }

    public Symbol ToBusinessEntity(CustomConfiguration.CustomConfiguration customConfiguration)
    {
        SymbolOverview overview = new()
        {
            Symbol = Symbol,
            Name = Name,
            Currency = CurrencyExtensions.ToEnum(Currency),
            DividendPerShareYearly = DividendPerShareYearly
        };

        if (customConfiguration.SymbolToMissingMarketCapInBillions.TryGetValue(Symbol, out double marketCapitalizationInBillions))
        {
            Console.WriteLine("Retrieved market cap [billion USD] for " + Symbol + " from the configuration (" + marketCapitalizationInBillions + ")");
            overview.MarketCapitalization = (long)(marketCapitalizationInBillions * 1e9);
        }
        else if (MarketCapitalization != 0)
        {
            overview.MarketCapitalization = MarketCapitalization;
        }
        else
        {
            throw new MissingMemberException(nameof(MarketCapitalization));
        }

        overview.InvestorRelationsWebsite = InvestorRelationsWebsite;
        List<SymbolDataPoint> symbolDataPointList = [];

        foreach (KeyValuePair<string,PyFiDataPoint> entry in HistoricalData)
        {
            SymbolDataPoint symbolDataPoint = new();
            symbolDataPoint.Date = DateOnly.ParseExact(entry.Key, "yyyy-MM-dd", null);
            symbolDataPoint.LowPrice = entry.Value.Low;
            symbolDataPoint.HighPrice = entry.Value.High;
            symbolDataPoint.MediumPrice = (entry.Value.Open + entry.Value.Close) / 2.0;

            symbolDataPointList.Add(symbolDataPoint);
        }

        Dictionary<DateOnly, double> dividendHistory = [];

        foreach (KeyValuePair<string, double> dividendPayout in DividendHistory)
        {
            DateOnly dividendPayoutDate = DateOnly.ParseExact(dividendPayout.Key, "yyyy-MM-dd", null);
            dividendHistory.Add(dividendPayoutDate, dividendPayout.Value);
        }

        SymbolInfoEx symbolInfoEx = new()
        {
            ForwardPE = ForwardPE,
            TrailingPE = TrailingPE,
            NumberOfAnalystOpinions = NumberOfAnalystOpinions,
            TargetMeanPrice = TargetMeanPrice
        };

        if (TargetMeanPrice == 0)
        {
            if(customConfiguration.SymbolToMissing1YearForecastMap.TryGetValue(Symbol, out double forecast))
            {
                Console.WriteLine("Retrieved forecast for " + Symbol + " from the configuration (" + forecast + ")");
                symbolInfoEx.TargetMeanPrice = forecast * symbolDataPointList.Last().MediumPrice;
                symbolInfoEx.NumberOfAnalystOpinions = 20;
            }
            else
            {
                throw new MissingMemberException(nameof(TargetMeanPrice));
            }
        }

        symbolInfoEx.RecommendationMean = ComputedRecommendationMean;
        if (ComputedRecommendationMean == 0)
        {
            symbolInfoEx.RecommendationMean = 1.0;
            Console.WriteLine("RecommendationMean for " + Symbol + " is missing and was set to 1.0!");
        }

        Symbol symbol = new([.. symbolDataPointList], overview, dividendHistory, symbolInfoEx);
        return symbol;
    }
}
﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.Api.ApiChart
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class ApiOverview
    {
        public ApiOverview() { }

        public SymbolOverview ToBusinessOverview()
        {
            SymbolOverview chartOverview = new SymbolOverview();
            chartOverview.Symbol = Symbol;
            chartOverview.Name = Name;
            chartOverview.MarketCapitalization = long.Parse(MarketCapitalization);

            if (string.Equals(PERatio, "None", StringComparison.InvariantCultureIgnoreCase))
            {
                chartOverview.PEratio = 0.0;
            }
            else
            {
                chartOverview.PEratio = double.Parse(PERatio);
            }

            if (string.Equals(DividendPerShare, "None", StringComparison.InvariantCultureIgnoreCase))
            {
                chartOverview.DividendPerShareYearly = 0.0;
            }
            else
            {
                chartOverview.DividendPerShareYearly = double.Parse(DividendPerShare);
            }

            if (Currency == "USD")
            {
                chartOverview.Currency = Enums.Currency.USD_US_DOLLAR;
            }
            else if (Currency == "EUR")
            {
                chartOverview.Currency = Enums.Currency.EUR_EURO;
            }
            else if (Currency == "GBP")
            {
                chartOverview.Currency = Enums.Currency.GBP_POUND_STERLING;
            }
            else if (Currency == "AUD")
            {
                chartOverview.Currency = Enums.Currency.AUD_AUSTRALIAN_DOLLAR;
            }
            else if (Currency == "CAD")
            {
                chartOverview.Currency = Enums.Currency.CAD_CANADIAN_DOLLAR;
            }
            else
            {
                throw new ArgumentException("Currency '" + Currency + "' is not recognized");
            }

            return chartOverview;
        }

        public string Symbol { get; set; }
        public string AssetType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CIK { get; set; }
        public string Exchange { get; set; }
        public string Currency { get; set; }
        public string Country { get; set; }
        public string Sector { get; set; }
        public string Industry { get; set; }
        public string Address { get; set; }
        public string FiscalYearEnd { get; set; }
        public string LatestQuarter { get; set; }
        public string MarketCapitalization { get; set; }
        public string EBITDA { get; set; }
        public string PERatio { get; set; }
        public string PEGRatio { get; set; }
        public string BookValue { get; set; }
        public string DividendPerShare { get; set; }
        public string DividendYield { get; set; }
        public string EPS { get; set; }
        public string RevenuePerShareTTM { get; set; }
        public string ProfitMargin { get; set; }
        public string OperatingMarginTTM { get; set; }
        public string ReturnOnAssetsTTM { get; set; }
        public string ReturnOnEquityTTM { get; set; }
        public string RevenueTTM { get; set; }
        public string GrossProfitTTM { get; set; }
        public string DilutedEPSTTM { get; set; }
        public string QuarterlyEarningsGrowthYOY { get; set; }
        public string QuarterlyRevenueGrowthYOY { get; set; }
        public string AnalystTargetPrice { get; set; }
        public string AnalystRatingStrongBuy { get; set; }
        public string AnalystRatingBuy { get; set; }
        public string AnalystRatingHold { get; set; }
        public string AnalystRatingSell { get; set; }
        public string AnalystRatingStrongSell { get; set; }
        public string TrailingPE { get; set; }
        public string ForwardPE { get; set; }
        public string PriceToSalesRatioTTM { get; set; }
        public string PriceToBookRatio { get; set; }
        public string EVToRevenue { get; set; }
        public string EVToEBITDA { get; set; }
        public string Beta { get; set; }

        [JsonProperty("52WeekHigh")]
        public string _52WeekHigh { get; set; }

        [JsonProperty("52WeekLow")]
        public string _52WeekLow { get; set; }

        [JsonProperty("50DayMovingAverage")]
        public string _50DayMovingAverage { get; set; }

        [JsonProperty("200DayMovingAverage")]
        public string _200DayMovingAverage { get; set; }
        public string SharesOutstanding { get; set; }
        public string DividendDate { get; set; }
        public string ExDividendDate { get; set; }
    }
}
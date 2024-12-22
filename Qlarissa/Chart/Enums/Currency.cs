using Qlarissa.Chart.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qlarissa.Chart.Enums
{
    public enum Currency
    {
        USD = 1,
        EUR = 2,
        GBP = 3,
        AUD = 4,
        CAD = 5,
        KRW = 6,
    }
}

public static class CurrencyExtensions
{
    public static Currency ToEnum(string currencyString)
    {
        if (Enum.TryParse(currencyString, out Currency currency))
        {
            return currency;
        }
        else
        {
            throw new ArgumentException(currencyString + " is unsupported.");
        }
    }
}
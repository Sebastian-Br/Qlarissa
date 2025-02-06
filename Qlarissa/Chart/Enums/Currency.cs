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
        USD = 1,    // US Dollar
        EUR = 2,    // Euro
        GBP = 3,    // British Pound
        AUD = 4,    // Australian Dollar
        CAD = 5,    // Canadian Dollar
        KRW = 6,    // Korean Won
        CHF = 7,    // Swiss Frank
        JPY = 8,    // Japense Yen
        SGD = 9,    // Singapore Dollar
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
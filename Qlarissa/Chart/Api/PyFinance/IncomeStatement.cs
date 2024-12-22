using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Qlarissa.Chart.Api.PyFinance;

public class IncomeStatement
{
    [JsonPropertyName("Total Revenue")]
    public double Revenue { get; set; }

    [JsonPropertyName("Net Income")]
    public double NetIncome { get; set; }
}
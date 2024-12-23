using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qlarissa.CustomConfiguration;

namespace Qlarissa.Chart.Api.PYfinance;

/// <summary>
/// Uses a local python API to query yfinance with the relevant python package
/// </summary>
internal class PyFinanceApiManager : IApiManager
{
    public PyFinanceApiManager(IConfiguration configuration, CustomConfiguration.CustomConfiguration customConfiguration)
    {
        DefaultStartDate = DateOnly.Parse(configuration.GetValue<string>("DefaultStartDate") ?? throw new ArgumentException(nameof(DefaultStartDate)));
        HttpClient = new();
        CustomConfiguration = customConfiguration;
    }

    private DateOnly DefaultStartDate { get; set; }

    private CustomConfiguration.CustomConfiguration CustomConfiguration { get; set; }

    private HttpClient HttpClient { get; set; }

    public async Task<Symbol> RetrieveSymbol(string ticker)
    {
        string endDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
        string apiUrl = $"http://localhost:5000/stock_data?ticker={ticker}&start_date={DefaultStartDate.ToString("yyyy-MM-dd")}&end_date={endDate}";
        var response = await HttpClient.GetAsync(apiUrl);
        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            PyFiSymbol pyFiSymbol = JsonConvert.DeserializeObject<PyFiSymbol>(jsonResponse);
            return pyFiSymbol.ToBusinessEntity(CustomConfiguration);
        }
        else
        {
            Console.WriteLine("Failed to fetch stock data: " + response.ReasonPhrase);
        }

        throw new InvalidOperationException();
    }
}
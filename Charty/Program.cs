using Charty.Chart;
using Charty.Chart.Api;
using Charty.Chart.ChartAnalysis;
using Charty.CustomConfiguration;
using Charty.Menu;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;


namespace Charty
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IHost host = CreateHostBuilder().Build();
            IConfiguration baseConfiguration = host.Services.GetRequiredService<IConfiguration>();
            CustomConfiguration.CustomConfiguration customConfiguration = BuildCustomConfiguration();
            // apiManager.GetApiSymbol("ETR:ADS").Wait(); WORKS

            //customConfiguration.CheckSymbolsToBeAnalyzed().Wait(); checked all except ETR:s
            SymbolManager symbolManager = new(baseConfiguration, customConfiguration);

            IMenu menu = new StartMenu(symbolManager);
            string input;

            while (true) {
                try
                {
                    input = Console.ReadLine();
                    menu = menu.SendText(input).Result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        static CustomConfiguration.CustomConfiguration BuildCustomConfiguration()
        {
            string fileContent = System.IO.File.ReadAllText("customConfiguration.json");
            return JsonConvert.DeserializeObject<CustomConfiguration.CustomConfiguration>(fileContent);
        }

        static IHostBuilder CreateHostBuilder()
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: true);
                //config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
            });

            return hostBuilder;
        }
    }
}
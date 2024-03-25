using Charty.Chart;
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

            SymbolManager chartManager = new(baseConfiguration, customConfiguration);

            IMenu menu = new StartMenu(chartManager);
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

            Console.WriteLine("Hello, World!");
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

        /*
         Unused Code Below
         */

        #region Incorrect Dictionary Deserialization
        // These functions all fail in the same way: They do NOT extract dictionary entries WHEN a StartDate or EndDate is null.
        // Additionally, they reorder the elements alphabetically according to their key. Why?
        CustomConfiguration.CustomConfiguration IConfiguration_Get_Fails(IConfiguration baseConfiguration)
        {
            return baseConfiguration.GetRequiredSection(nameof(CustomConfiguration.CustomConfiguration)).Get<CustomConfiguration.CustomConfiguration>(); 
        }

        CustomConfiguration.CustomConfiguration ConfigurationBinder_Get_Fails(IConfiguration baseConfiguration)
        {
            return (CustomConfiguration.CustomConfiguration)ConfigurationBinder.Get(baseConfiguration, typeof(CustomConfiguration.CustomConfiguration));
        }

        CustomConfiguration.CustomConfiguration IOptions_Fails(IConfiguration baseConfiguration)
        {
            var services = new ServiceCollection();

            services.AddOptions<CustomConfiguration.CustomConfiguration>()
                .Bind(baseConfiguration.GetSection(nameof(CustomConfiguration.CustomConfiguration)));

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<IOptions<CustomConfiguration.CustomConfiguration>>().Value;
        }

        #endregion

        static Dictionary<TKey, TValue> GetDictionaryFromConfiguration<TKey,TValue>(IConfiguration configuration,string key) // this is required because by default, config.GetValue<string,any:object> doesn't deserialize the dictionary correctly for any constructed object type
        {
            IConfigurationSection section = configuration.GetSection(key);
            var dictionary = GetSectionAsDictionary(section);
            string defaultExcludedTimePeriodsString = JsonConvert.SerializeObject(dictionary);
            Dictionary<TKey, TValue> resultDictionary = JsonConvert.DeserializeObject<Dictionary<TKey, TValue>>(defaultExcludedTimePeriodsString);
            return resultDictionary;
        }
        static Dictionary<string, object> GetSectionAsDictionary(IConfigurationSection section)
        {
            var result = new Dictionary<string, object>();

            foreach (var child in section.GetChildren())
            {
                if (child.Value != null)
                {
                    result[child.Key] = child.Value;
                }
                else
                {
                    var childDictionary = GetSectionAsDictionary(child);
                    result[child.Key] = childDictionary.Any() ? childDictionary : null;
                }
            }

            return result;
        }
    }
}
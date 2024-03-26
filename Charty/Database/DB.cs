using Charty.Chart;
using Charty.Chart.ChartAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Database
{
    public class DB
    {
        public DB(IConfiguration configuration)
        {
            string dbFileName = configuration.GetValue<string>("DataBaseFile");
            if (string.IsNullOrEmpty(dbFileName))
            {
                throw new ArgumentException("No db file name provided.");
            }
            Connection = new SqliteConnection(dbFileName);
            Connection.Open();
            SetupTables();
        }

        private SqliteConnection Connection { get; set; }

        private void SetupTables()
        {
            TransactCommand(createTable_Symbols);
            TransactCommand(createTable_SymbolDataPoints);
            TransactCommand(createTable_SymbolOverviews);
            TransactCommand(createTable_ExponentialRegressionResults);
        }

        private void TransactCommand(string commandText)
        {
            using (SqliteTransaction transaction = Connection.BeginTransaction())
            {
                try
                {
                    using (SqliteCommand command = new SqliteCommand(commandText, Connection, transaction))
                    {
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine("Transaction rolled back due to an exception: " + ex);
                }
            }
        }

        public void InsertOrUpdateSymbolInformation(Symbol symbol)
        {
            throw new NotImplementedException();
        }

        private string Select_SymbolId(string symbol)
        {
            string command = "SELECT Id FROM " + Table_Symbols + " WHERE Symbol COLLATE NOCASE = " + SingleQuote(symbol)  + ";";
            return command;
        }

        private static string Table_Symbols = "Symbols";
        private static string Table_ExponentialRegressionResults = "ExponentialRegressionResults";
        private static string Table_SymbolOverviews = "SymbolOverviews";
        private static string Table_SymbolDataPoints = "SymbolDataPoints";

        private readonly string createTable_Symbols = "CREATE TABLE IF NOT EXISTS " + Table_Symbols +
            " (Id INTEGER PRIMARY KEY AUTOINCREMENT, Symbol TEXT)";

        private readonly string createTable_SymbolDataPoints = "CREATE TABLE IF NOT EXISTS " + Table_SymbolDataPoints +
            " (SymbolId INTEGER," +
            "Date DATE," +
            "LowPrice REAL," +
            "MediumPrice REAL," +
            "HighPrice REAL," +
            "PRIMARY KEY (SymbolId, Date));"; // Unique constraint on the combination of SymbolId and Date

        private readonly string createTable_SymbolOverviews = "CREATE TABLE IF NOT EXISTS " + Table_SymbolOverviews + 
            " (SymbolId INTEGER PRIMARY KEY," +
            "Name TEXT," +
            "Currency INTEGER," +
            "MarketCapitalization INTEGER," +
            "PEratio REAL," +
            "DividendPerShareYearly REAL);";

        private readonly string createTable_ExponentialRegressionResults = "CREATE TABLE IF NOT EXISTS " + Table_ExponentialRegressionResults +
            " (SymbolId INTEGER PRIMARY KEY," +
            "A REAL," +
            "B REAL," +
            "OneYearGrowthEstimatePercentage REAL," +
            "ThreeYearGrowthEstimatePercentage REAL," +
            "CurrentPrice REAL," +
            "DateCreated DATE);";

        private string InsertInto_Symbols_IfNotExists(string symbol)
        {
            string sqlCommand = "INSERT OR IGNORE INTO " + Table_Symbols + " (symbol) " +
                "SELECT \"" + symbol +"\" " +
                "WHERE NOT EXISTS " +
                "(SELECT 1 FROM Symbols WHERE LOWER(symbol) = LOWER(\"" + symbol + "\"));";
            return sqlCommand;
        }

        private string InsertInto_SymbolDataPoints_IfNotExists(int symbolId, SymbolDataPoint dataPoint)
        {
            string sqlCommand = "INSERT OR IGNORE INTO " + Table_SymbolDataPoints + 
                " (SymbolId, Date, LowPrice, MediumPrice, HighPrice) " +
                CreateValuesString(symbolId, dataPoint);
            return sqlCommand;
        }

        private string InsertInto_SymbolOverviews_OrReplace(int symbolId, SymbolOverview overview)
        {
            string sqlCommand = "INSERT OR REPLACE INTO " + Table_SymbolOverviews +
                " (SymbolId, Name, Currency, MarketCapitalization, PEratio, DividendPerShareYearly) " +
                CreateValuesString(symbolId, overview);
            return sqlCommand;
        }

        private string InsertInto_ExponentialRegressionResults_OrReplace(int symbolId, ExponentialRegressionResult result)
        {
            string sqlCommand = "INSERT OR REPLACE INTO ExponentialRegressionResults" +
                " (SymbolId, A, B, OneYearGrowthEstimatePercentage, ThreeYearGrowthEstimatePercentage, CurrentPrice, DateCreated) " +
                CreateValuesString(symbolId, result);
                ;
            return sqlCommand;
        }

        private string CreateValuesString(int symbolId, SymbolDataPoint dataPoint)
        {
            string partialCommand = "VALUES (" +
                symbolId + ", " +
                SingleQuote(dataPoint.Date) + ", " +
                dataPoint.LowPrice + ", " +
                dataPoint.MediumPrice + ", " +
                dataPoint.HighPrice + ");";
                ;

            return partialCommand;
        }

        private string CreateValuesString(int symbolId, SymbolOverview overview)
        {
            string partialCommand = "VALUES (" +
                symbolId + ", " +
                SingleQuote(overview.Name) + ", " +
                overview.Currency + ", " +
                overview.MarketCapitalization + ", " +
                overview.PEratio + ", " +
                overview.DividendPerShareYearly +
                ");"
                ;

            return partialCommand;
        }

        private string CreateValuesString(int symbolId, ExponentialRegressionResult result)
        {
            string partialCommand = "VALUES (" +
                symbolId + ", " +
                result.A + ", " +
                result.B + ", " +
                result.OneYearGrowthEstimatePercentage + ", " +
                result.ThreeYearGrowthEstimatePercentage + ", " +
                result.CurrentPrice + ", " +
                SingleQuote(result.DateCreated) + 
                ");";

            return partialCommand;
        }

        private string SingleQuote(string text)
        {
            return "'" + text + "'";
        }

        private string SingleQuote(DateOnly dateOnly)
        {
            return "'" + dateOnly.ToString() + "'";
        }
    }
}
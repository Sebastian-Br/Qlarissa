using Charty.Chart;
using Charty.Chart.Analysis.ExponentialRegression;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

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
            Connection = new SqliteConnection("Data Source=" + dbFileName);
            Connection.Open();
            SetupTables();
        }

        private SqliteConnection Connection { get; set; }

        #region Readers
        public Dictionary<string, Symbol> LoadSymbolDictionary()
        {
            Dictionary<string, Symbol> symbolDictionary = new();

            Dictionary<int, string> symbolsTable = ReadSymbolsTable();
            Dictionary<int, SymbolOverview> overviewTable = ReadSymbolOverviewsTable(symbolsTable);
            Dictionary<(int, DateOnly), SymbolDataPoint> symbolDataPointsTable = ReadSymbolDataPointsTable();
            Dictionary<int, ExponentialRegressionResult> exponentialRegressionResultsTable = ReadExponentialRegressionResultsTable(overviewTable);

            foreach (KeyValuePair<int, string> symbolTableEntry in symbolsTable)
            {
                SymbolDataPoint[] sortedDataPoints = symbolDataPointsTable
                .Where(pair => pair.Key.Item1 == symbolTableEntry.Key) // Filter by SymbolId
                .OrderBy(pair => pair.Key.Item2) // Sort by Date in ascending order
                .Select(pair => pair.Value) // Select SymbolDataPoints
                .ToArray();

                SymbolOverview overview = overviewTable[symbolTableEntry.Key];
                ExponentialRegressionResult expRegressionResult = null;

                if (exponentialRegressionResultsTable.ContainsKey(symbolTableEntry.Key))
                {
                    expRegressionResult = exponentialRegressionResultsTable[symbolTableEntry.Key];
                }

                Symbol symbol = new(dataPoints: sortedDataPoints,
                    overview: overview,
                    exponentialRegressionResult : expRegressionResult
                    );

                symbolDictionary.Add(symbol.Overview.Symbol, symbol);
            }

            return symbolDictionary;
        }

        private Dictionary<int, string> ReadSymbolsTable()
        {
            Dictionary<int, string> symbols = new();
            var command = Connection.CreateCommand();
            command.CommandText = "SELECT Id, Symbol FROM Symbols";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    symbols.Add(reader.GetInt32(0), reader.GetString(1));
                }
            }

            return symbols;
        }

        private Dictionary<int, SymbolOverview> ReadSymbolOverviewsTable(Dictionary<int, string> symbolsTable)
        {
            Dictionary<int, SymbolOverview> overviewDictionary = new();
            var command = Connection.CreateCommand();
            command.CommandText = "SELECT SymbolId, Name, Currency, MarketCapitalization, PEratio, DividendPerShareYearly FROM SymbolOverviews";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    SymbolOverview overview = new SymbolOverview();
                    overview.Symbol = symbolsTable[reader.GetInt32(0)];
                    overview.Name = reader.GetString(1);
                    overview.Currency = (Chart.Enums.Currency)reader.GetInt32(2);
                    overview.MarketCapitalization = reader.GetInt64(3);
                    overview.PEratio = reader.GetDouble(4);
                    overview.DividendPerShareYearly = reader.GetDouble(5);
                    overviewDictionary.Add(reader.GetInt32(0), overview);
                }
            }

            return overviewDictionary;
        }

        private Dictionary<(int, DateOnly), SymbolDataPoint> ReadSymbolDataPointsTable()
        {
            var symbolDataPoints = new Dictionary<(int, DateOnly), SymbolDataPoint>();

            var command = Connection.CreateCommand();
            command.CommandText = "SELECT SymbolId, Date, LowPrice, MediumPrice, HighPrice FROM SymbolDataPoints";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int symbolId = reader.GetInt32(0);
                    DateOnly date = DateOnly.FromDateTime(reader.GetDateTime(1));

                    SymbolDataPoint dataPoint = new SymbolDataPoint
                    {
                        Date = date,
                        LowPrice = reader.GetDouble(2),
                        MediumPrice = reader.GetDouble(3),
                        HighPrice = reader.GetDouble(4)
                    };

                    symbolDataPoints.Add((symbolId, date), dataPoint);
                    //symbolDataPoints[(symbolId, date)] = dataPoint; add-or-update
                }
            }

            return symbolDataPoints;
        }

        private Dictionary<int, ExponentialRegressionResult> ReadExponentialRegressionResultsTable(Dictionary<int, SymbolOverview> overviewTable)
        {
            Dictionary<int, ExponentialRegressionResult> expResultTable = new();
            var command = Connection.CreateCommand();
            command.CommandText = "SELECT SymbolId, A, B, OneYearGrowthEstimatePercentage, ThreeYearGrowthEstimatePercentage, CurrentPrice, DateCreated FROM ExponentialRegressionResults";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int symbolId = reader.GetInt32(0);
                    ExponentialRegressionResult result = new(
                        _A : reader.GetDouble(1),
                        _B : reader.GetDouble(2),
                        _OneYearGrowthEstimatePercentage : reader.GetDouble(3),
                        _ThreeYearGrowthEstimatePercentage : reader.GetDouble(4),
                        _CurrentPrice : reader.GetDouble(5),
                        _DateCreated : DateOnly.FromDateTime(reader.GetDateTime(6)),
                        _Overview : overviewTable[symbolId]
                        );

                    expResultTable.Add( symbolId, result );
                }
            }

            return expResultTable;
        }

        #endregion Readers

        private void SetupTables()
        {
            TransactCommand(createTable_Symbols);
            TransactCommand(createTable_SymbolDataPoints);
            TransactCommand(createTable_SymbolOverviews);
            TransactCommand(createTable_ExponentialRegressionResults);
        }

        private bool TransactCommand(string command)
        {
            using (SqliteTransaction transaction = Connection.BeginTransaction())
            {
                try
                {
                    using (SqliteCommand sqlCommand = new SqliteCommand(command, Connection, transaction))
                    {
                        sqlCommand.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine("Transaction rolled back due to an exception: " + ex);
                }
            }

            return false;
        }

        private bool TransactCommands(List<string> commands)
        {
            using (SqliteTransaction transaction = Connection.BeginTransaction())
            {
                try
                {
                    string allCommands = string.Join("\n", commands);
                    foreach(string command in commands)
                    {
                        using (SqliteCommand sqlCommand = new SqliteCommand(command, Connection, transaction))
                        {
                            sqlCommand.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine("Transaction rolled back due to an exception: " + ex);
                }
            }

            return false;
        }

        public void InsertOrUpdateSymbolInformation(Symbol symbol)
        {
            return; // REMOVE LATER
            List<string> commands = new();
            commands.Add(InsertInto_Symbols_IfNotExists(symbol.Overview.Symbol));
            commands.Add(Declare_SYMBOL_ID_FromSymbolsTable(symbol.Overview.Symbol));

            foreach(SymbolDataPoint dataPoint in symbol.DataPoints)
            {
                commands.Add(InsertInto_SymbolDataPoints_IfNotExists(dataPoint));
            }

            commands.Add(InsertInto_SymbolOverviews_OrReplace(symbol.Overview));

            if(symbol.ExponentialRegressionModel != null)
            {
                commands.Add(InsertInto_ExponentialRegressionResults_OrReplace(symbol.ExponentialRegressionModel));
            }

            commands.Add("DROP TABLE IF EXISTS " + SYMBOL_ID_NAME + ";");

            TransactCommands(commands);
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

        
        private static readonly string SYMBOL_ID_NAME = "LOCAL_SYMBOL_ID";

        /// <summary>
        /// The Sqlite-local used in the other commands to update data for a symbol
        /// </summary>
        private readonly string SYMBOL_ID = "(SELECT * FROM " + SYMBOL_ID_NAME + ")";

        private string Declare_SYMBOL_ID_FromSymbolsTable(string symbol)
        {
            string command = "CREATE TEMPORARY TABLE IF NOT EXISTS " + SYMBOL_ID_NAME + "(Id INTEGER); " +
                "INSERT INTO " + SYMBOL_ID_NAME + " (Id) " + "SELECT Id FROM " + Table_Symbols + " WHERE Symbol COLLATE NOCASE = " + SingleQuote(symbol) + ";";
            //string command = "WITH " + SYMBOL_ID_NAME + " AS (SELECT Id FROM " + Table_Symbols + " WHERE Symbol COLLATE NOCASE = " + SingleQuote(symbol) + ");";
            return command;
        }

        private string InsertInto_Symbols_IfNotExists(string symbol)
        {
            string sqlCommand = "INSERT OR IGNORE INTO " + Table_Symbols + " (symbol) " +
                "SELECT UPPER(\"" + symbol +"\") " +
                "WHERE NOT EXISTS " +
                "(SELECT 1 FROM Symbols WHERE Symbol = UPPER(\"" + symbol + "\"));";
            return sqlCommand;
        }

        private string InsertInto_SymbolDataPoints_IfNotExists(SymbolDataPoint dataPoint)
        {
            string sqlCommand = "INSERT OR IGNORE INTO " + Table_SymbolDataPoints + 
                " (SymbolId, Date, LowPrice, MediumPrice, HighPrice) " +
                CreateValuesString(dataPoint);
            return sqlCommand;
        }

        private string InsertInto_SymbolOverviews_OrReplace(SymbolOverview overview)
        {
            string sqlCommand = "INSERT OR REPLACE INTO " + Table_SymbolOverviews +
                " (SymbolId, Name, Currency, MarketCapitalization, PEratio, DividendPerShareYearly) " +
                CreateValuesString(overview);
            return sqlCommand;
        }

        private string InsertInto_ExponentialRegressionResults_OrReplace(ExponentialRegressionResult result)
        {
            string sqlCommand = "INSERT OR REPLACE INTO ExponentialRegressionResults" +
                " (SymbolId, A, B, OneYearGrowthEstimatePercentage, ThreeYearGrowthEstimatePercentage, CurrentPrice, DateCreated) " +
                CreateValuesString(result);
                ;
            return sqlCommand;
        }

        private string CreateValuesString(SymbolDataPoint dataPoint)
        {
            string partialCommand = "VALUES (" +
                SYMBOL_ID + ", " +
                SingleQuote(dataPoint.Date) + ", " +
                dataPoint.LowPrice + ", " +
                dataPoint.MediumPrice + ", " +
                dataPoint.HighPrice + ");";
                ;

            return partialCommand;
        }

        private string CreateValuesString(SymbolOverview overview)
        {
            string partialCommand = "VALUES (" +
                SYMBOL_ID + ", " +
                SingleQuote(overview.Name) + ", " +
                (int)overview.Currency + ", " +
                overview.MarketCapitalization + ", " +
                overview.PEratio + ", " +
                overview.DividendPerShareYearly +
                ");"
                ;

            return partialCommand;
        }

        private string CreateValuesString(ExponentialRegressionResult result)
        {
            string partialCommand = "VALUES (" +
                SYMBOL_ID + ", " +
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
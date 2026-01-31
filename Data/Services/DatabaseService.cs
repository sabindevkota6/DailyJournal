using DailyJournal.Core.Services;
using DailyJournal.Data.Utils;
using SQLite;

namespace DailyJournal.Data.Services
{
    // service class that manages the SQLite database connection
    public class DatabaseService : IDatabaseService
    {
        // private field to hold the database connection
        private SQLiteAsyncConnection? _connection;

        // property to get the database connection, initializing if needed
        public SQLiteAsyncConnection Connection
        {
            get
            {
                try
                {
                    // initializing connection if it has not been created yet
                    if (_connection == null)
                    {
                        InitializeAsync().GetAwaiter().GetResult();
                    }

                    return _connection!;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error getting database connection: {ex.Message}");
                    throw new ApplicationException("Failed to get database connection", ex);
                }
            }
        }

        // initializing the database connection asynchronously
        public async Task InitializeAsync()
        {
            try
            {
                // skipping if already initialized
                if (_connection != null)
                {
                    return;
                }

                // getting the database file path
                var path = await DbConfig.GetPathAsync();
                System.Diagnostics.Debug.WriteLine($"Initializing database at path: {path}");

                // creating the SQLite connection
                _connection = new SQLiteAsyncConnection(path);

                System.Diagnostics.Debug.WriteLine("Database initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing database: {ex.Message}");
                throw new ApplicationException("Failed to initialize database", ex);
            }
        }
    }
}

using DailyJournal.Core.Services;
using DailyJournal.Data.Utils;
using SQLite;
using System;
using System.Threading.Tasks;

namespace DailyJournal.Data.Services
{
    public class DatabaseService : IDatabaseService
    {
        private SQLiteAsyncConnection? _connection;

        public SQLiteAsyncConnection Connection
        {
            get
            {
                try
                {
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

        public async Task InitializeAsync()
        {
            try
            {
                if (_connection != null)
                {
                    return;
                }

                var path = await DbConfig.GetPathAsync();
                System.Diagnostics.Debug.WriteLine($"Initializing database at path: {path}");

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

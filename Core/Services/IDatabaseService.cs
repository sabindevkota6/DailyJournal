using SQLite;

namespace DailyJournal.Core.Services
{
    // interface for database operations
    public interface IDatabaseService
    {
        // getting the SQLite async connection for database queries
        SQLiteAsyncConnection Connection { get; }

        // initializing the database and creating tables if needed
        Task InitializeAsync();
    }
}

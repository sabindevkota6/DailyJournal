namespace DailyJournal.Data.Utils
{
    // static class for database configuration and path management
    public static class DbConfig
    {
        // name of the SQLite database file
        private const string DbName = "DailyJournal.db3";

        // key used to store the database path in secure storage
        private const string DbPathKey = "db_path";

        // getting the database file path, creating it if it does not exist
        public static async Task<string> GetPathAsync()
        {
            // trying to get existing path from secure storage
            var path = await SecureStorage.GetAsync(DbPathKey);

            // if no path exists, creating one in the app data directory
            if (string.IsNullOrEmpty(path))
            {
                path = Path.Combine(FileSystem.AppDataDirectory, DbName);
                await SecureStorage.SetAsync(DbPathKey, path);
            }

            return path;
        }
    }
}

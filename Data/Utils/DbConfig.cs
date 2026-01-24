using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DailyJournal.Data.Utils
{
    public static class DbConfig
    {
        private const string DbName = "DailyJournal.db3";
        private const string DbPathKey = "db_path";

        public static async Task<string> GetPathAsync()
        {
            var path = await SecureStorage.GetAsync(DbPathKey);

            if (string.IsNullOrEmpty(path))
            {
                path = Path.Combine(FileSystem.AppDataDirectory, DbName);
                await SecureStorage.SetAsync(DbPathKey, path);
            }
            return path;
        }
    }
}

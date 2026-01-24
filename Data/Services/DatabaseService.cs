using DailyJournal.Core.Services;
using DailyJournal.Data.Utils;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                if (_connection == null)
                    InitializeAsync().GetAwaiter().GetResult();

                return _connection!;
            }
        }

        public async Task InitializeAsync()
        {
            if (_connection != null) return;

            var path = await DbConfig.GetPathAsync();
            _connection = new SQLiteAsyncConnection(path);
        }
    }
}

using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DailyJournal.Core.Services
{
    public interface IDatabaseService
    {
        SQLiteAsyncConnection Connection { get; }
        Task InitializeAsync();
    }
}

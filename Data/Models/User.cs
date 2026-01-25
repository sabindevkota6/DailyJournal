using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace DailyJournal.Data.Models
{
    public class User
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public int Pin { get; set; }
    }
}

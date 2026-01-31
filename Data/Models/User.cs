using SQLite;

namespace DailyJournal.Data.Models
{
    // model class representing a user in the database
    public class User
    {
        // unique identifier for the user
        [PrimaryKey]
        public Guid Id { get; set; }

        // user PIN for authentication (stored as integer)
        public int Pin { get; set; }
    }
}

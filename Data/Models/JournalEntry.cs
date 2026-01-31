using SQLite;
using System.Text.Json;

namespace DailyJournal.Data.Models
{
    // enum for primary mood categories
    public enum Category
    {
        Positive,
        Neutral,
        Negative
    }

    // enum for specific mood types within each category
    public enum Mood
    {
        // positive moods
        Happy,
        Excited,
        Relaxed,
        Grateful,
        Confident,

        // neutral moods
        Calm,
        Thoughtful,
        Curious,
        Nostalgic,
        Bored,

        // negative moods
        Sad,
        Angry,
        Stressed,
        Lonely,
        Anxious
    }

    // model class representing a journal entry in the database
    [Table("JournalEntries")]
    public class JournalEntry
    {
        // unique identifier for the entry
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();

        // date of the entry, only one entry allowed per day
        [Indexed(Unique = true)]
        public DateTime Date { get; set; }

        // title of the journal entry
        public string Title { get; set; } = string.Empty;

        // main content of the entry (HTML or Markdown)
        public string Content { get; set; } = string.Empty;

        // true if content is Markdown, false if HTML from Quill editor
        public bool IsMarkdown { get; set; }

        // primary mood stored for database compatibility (used in analytics)
        public Mood PrimaryMood { get; set; }

        // JSON string storing secondary moods array
        public string SecondaryMoodsJson { get; set; } = "[]";

        // the actual primary mood category selected by user
        public Category? Category { get; set; }

        // JSON string storing tags array
        public string TagsJson { get; set; } = "[]";

        // timestamp when entry was created
        public DateTime CreatedAt { get; set; }

        // timestamp when entry was last updated
        public DateTime UpdatedAt { get; set; }

        // computed property to get/set secondary moods as a list
        [Ignore]
        public List<Mood> SecondaryMoods
        {
            get => JsonSerializer.Deserialize<List<Mood>>(SecondaryMoodsJson) ?? new();
            set => SecondaryMoodsJson = JsonSerializer.Serialize(value ?? new());
        }

        // computed property to get/set tags as a list
        [Ignore]
        public List<string> Tags
        {
            get => JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new();
            set => TagsJson = JsonSerializer.Serialize(value ?? new());
        }
    }
}

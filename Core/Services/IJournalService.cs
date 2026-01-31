using DailyJournal.Data.Models;

namespace DailyJournal.Core.Services
{
    // interface for journal entry operations
    public interface IJournalService
    {
        // getting a journal entry by its unique ID
        Task<JournalEntry?> GetEntryByIdAsync(Guid id);

        // getting a journal entry by date (one entry per day)
        Task<JournalEntry?> GetEntryByDateAsync(DateTime date);

        // creating a new journal entry with mood and tag information
        Task<JournalEntry> CreateEntryAsync(DateTime date, string title, string content, bool isMarkdown, Mood primaryMood, IEnumerable<Mood>? secondaryMoods = null, Category? category = null, IEnumerable<string>? tags = null);

        // updating an existing journal entry by ID
        Task<JournalEntry> UpdateEntryAsync(Guid id, string title, string content, bool isMarkdown, Mood primaryMood, IEnumerable<Mood>? secondaryMoods = null, Category? category = null, IEnumerable<string>? tags = null);

        // creating a new entry or updating existing one for the given date
        Task<JournalEntry> CreateOrUpdateEntryAsync(DateTime date, string title, string content, bool isMarkdown, Mood primaryMood, IEnumerable<Mood>? secondaryMoods = null, Category? category = null, IEnumerable<string>? tags = null);

        // deleting an entry by date
        Task<bool> DeleteEntryAsync(DateTime date);

        // deleting an entry by its ID
        Task<bool> DeleteEntryByIdAsync(Guid id);

        // searching entries with optional filters for query, date range, moods, and tags
        Task<List<JournalEntry>> SearchAsync(string? query = null, DateTime? start = null, DateTime? end = null, IEnumerable<Mood>? moods = null, IEnumerable<string>? tags = null);

        // getting the current journaling streak in days
        Task<int> GetCurrentStreakAsync();
    }
}

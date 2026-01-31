using DailyJournal.Core.Services;
using DailyJournal.Data.Models;
using SQLite;

namespace DailyJournal.Data.Services
{
    // service class for managing journal entries
    public class JournalService : IJournalService
    {
        // database service dependency
        private readonly IDatabaseService _databaseService;

        // constructor injecting the database service
        public JournalService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        // ensuring the JournalEntry table exists in the database
        private async Task InitializeTableAsync()
        {
            try
            {
                await _databaseService.Connection.CreateTableAsync<JournalEntry>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing JournalEntry table: {ex.Message}");
                throw;
            }
        }

        // normalizing a date to midnight (removing time component)
        private static DateTime NormalizeDate(DateTime date) => date.Date;

        // getting a journal entry by its unique ID
        public async Task<JournalEntry?> GetEntryByIdAsync(Guid id)
        {
            try
            {
                await InitializeTableAsync();
                return await _databaseService.Connection.Table<JournalEntry>()
                    .Where(e => e.Id == id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetEntryByIdAsync: {ex.Message}");
                throw new ApplicationException("GetEntryByIdAsync failed", ex);
            }
        }

        // getting a journal entry by its date
        public async Task<JournalEntry?> GetEntryByDateAsync(DateTime date)
        {
            try
            {
                await InitializeTableAsync();
                var d = NormalizeDate(date);
                return await _databaseService.Connection.Table<JournalEntry>()
                    .Where(e => e.Date == d)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetEntryByDateAsync: {ex.Message}");
                throw new ApplicationException("GetEntryByDateAsync failed", ex);
            }
        }

        // creating a new journal entry
        public async Task<JournalEntry> CreateEntryAsync(
            DateTime date,
            string title,
            string content,
            bool isMarkdown,
            Mood primaryMood,
            IEnumerable<Mood>? secondaryMoods = null,
            Category? category = null,
            IEnumerable<string>? tags = null)
        {
            try
            {
                await InitializeTableAsync();

                var d = NormalizeDate(date);
                var now = DateTime.Now;

                System.Diagnostics.Debug.WriteLine($"Creating entry for date: {d:yyyy-MM-dd} (Local: {DateTime.Now:yyyy-MM-dd HH:mm:ss})");

                // checking if entry already exists for this date
                var existing = await GetEntryByDateAsync(d);
                if (existing != null)
                {
                    throw new InvalidOperationException($"An entry already exists for date {d:yyyy-MM-dd}.");
                }

                // processing secondary moods and limiting to 2 unique moods
                var secondary = (secondaryMoods ?? Enumerable.Empty<Mood>())
                    .Distinct()
                    .Take(2)
                    .ToList();

                // normalizing tags by trimming whitespace and removing duplicates
                var normalizedTags = (tags ?? Enumerable.Empty<string>())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // creating the entry object
                var entry = new JournalEntry
                {
                    Date = d,
                    Title = title ?? string.Empty,
                    Content = content ?? string.Empty,
                    IsMarkdown = isMarkdown,
                    PrimaryMood = primaryMood,
                    SecondaryMoods = secondary,
                    Category = category,
                    Tags = normalizedTags,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _databaseService.Connection.InsertAsync(entry);
                System.Diagnostics.Debug.WriteLine($"Entry created successfully for {d:yyyy-MM-dd}");
                return entry;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateEntryAsync error: {ex}");
                throw new ApplicationException("CreateEntryAsync failed", ex);
            }
        }

        // updating an existing journal entry by ID
        public async Task<JournalEntry> UpdateEntryAsync(
            Guid id,
            string title,
            string content,
            bool isMarkdown,
            Mood primaryMood,
            IEnumerable<Mood>? secondaryMoods = null,
            Category? category = null,
            IEnumerable<string>? tags = null)
        {
            try
            {
                await InitializeTableAsync();

                // finding the existing entry
                var existing = await GetEntryByIdAsync(id);
                if (existing == null)
                {
                    throw new KeyNotFoundException($"Journal entry with id '{id}' was not found.");
                }

                var now = DateTime.Now;

                System.Diagnostics.Debug.WriteLine($"Updating entry {id} (Date: {existing.Date:yyyy-MM-dd})");

                // processing secondary moods and limiting to 2 unique moods
                var secondary = (secondaryMoods ?? Enumerable.Empty<Mood>())
                    .Distinct()
                    .Take(2)
                    .ToList();

                // normalizing tags by trimming whitespace and removing duplicates
                var normalizedTags = (tags ?? Enumerable.Empty<string>())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // updating the entry fields
                existing.Title = title ?? string.Empty;
                existing.Content = content ?? string.Empty;
                existing.IsMarkdown = isMarkdown;
                existing.PrimaryMood = primaryMood;
                existing.SecondaryMoods = secondary;
                existing.Category = category;
                existing.Tags = normalizedTags;
                existing.UpdatedAt = now;

                await _databaseService.Connection.UpdateAsync(existing);
                System.Diagnostics.Debug.WriteLine($"Entry updated successfully");
                return existing;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateEntryAsync error: {ex}");
                throw new ApplicationException("UpdateEntryAsync failed", ex);
            }
        }

        // creating a new entry or updating existing one for backward compatibility
        public async Task<JournalEntry> CreateOrUpdateEntryAsync(
            DateTime date,
            string title,
            string content,
            bool isMarkdown,
            Mood primaryMood,
            IEnumerable<Mood>? secondaryMoods = null,
            Category? category = null,
            IEnumerable<string>? tags = null)
        {
            try
            {
                await InitializeTableAsync();

                var d = NormalizeDate(date);
                var existing = await GetEntryByDateAsync(d);

                if (existing == null)
                {
                    try
                    {
                        return await CreateEntryAsync(d, title, content, isMarkdown, primaryMood, secondaryMoods, category, tags);
                    }
                    catch (SQLiteException)
                    {
                        // handling race condition with unique date index
                        var again = await GetEntryByDateAsync(d);
                        if (again == null) throw;

                        return await UpdateEntryAsync(again.Id, title, content, isMarkdown, primaryMood, secondaryMoods, category, tags);
                    }
                }

                return await UpdateEntryAsync(existing.Id, title, content, isMarkdown, primaryMood, secondaryMoods, category, tags);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateOrUpdateEntryAsync error: {ex}");
                throw new ApplicationException("CreateOrUpdateEntryAsync failed", ex);
            }
        }

        // deleting an entry by date
        public async Task<bool> DeleteEntryAsync(DateTime date)
        {
            try
            {
                await InitializeTableAsync();
                var d = NormalizeDate(date);
                var existing = await GetEntryByDateAsync(d);
                if (existing == null) return false;
                await _databaseService.Connection.DeleteAsync(existing);
                System.Diagnostics.Debug.WriteLine($"Entry deleted for date: {d:yyyy-MM-dd}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteEntryAsync error: {ex}");
                throw new ApplicationException("DeleteEntryAsync failed", ex);
            }
        }

        // deleting an entry by its ID
        public async Task<bool> DeleteEntryByIdAsync(Guid id)
        {
            try
            {
                await InitializeTableAsync();
                var existing = await GetEntryByIdAsync(id);
                if (existing == null) return false;
                await _databaseService.Connection.DeleteAsync(existing);
                System.Diagnostics.Debug.WriteLine($"Entry deleted with id: {id}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteEntryByIdAsync error: {ex}");
                throw new ApplicationException("DeleteEntryByIdAsync failed", ex);
            }
        }

        // searching entries with optional filters
        public async Task<List<JournalEntry>> SearchAsync(
            string? query = null,
            DateTime? start = null,
            DateTime? end = null,
            IEnumerable<Mood>? moods = null,
            IEnumerable<string>? tags = null)
        {
            try
            {
                await InitializeTableAsync();

                // getting all entries first since SQLite net does not support complex queries
                var list = await _databaseService.Connection.Table<JournalEntry>().ToListAsync();
                var q = list.AsEnumerable();

                // filtering by search query in title or content
                if (!string.IsNullOrWhiteSpace(query))
                {
                    var needle = query.Trim();
                    q = q.Where(e =>
                        (e.Title ?? string.Empty).Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                        (e.Content ?? string.Empty).Contains(needle, StringComparison.OrdinalIgnoreCase));
                }

                // filtering by start date
                if (start.HasValue)
                {
                    var s = NormalizeDate(start.Value);
                    q = q.Where(e => e.Date >= s);
                }

                // filtering by end date
                if (end.HasValue)
                {
                    var e = NormalizeDate(end.Value);
                    q = q.Where(x => x.Date <= e);
                }

                // filtering by moods (primary or secondary)
                var moodSet = (moods ?? Enumerable.Empty<Mood>()).Distinct().ToHashSet();
                if (moodSet.Count > 0)
                {
                    q = q.Where(e => moodSet.Contains(e.PrimaryMood) || e.SecondaryMoods.Any(moodSet.Contains));
                }

                // filtering by tags
                var tagSet = (tags ?? Enumerable.Empty<string>())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (tagSet.Count > 0)
                {
                    q = q.Where(e => e.Tags.Any(tagSet.Contains));
                }

                // returning results sorted by date descending
                return q.OrderByDescending(e => e.Date).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SearchAsync error: {ex}");
                throw new ApplicationException("SearchAsync failed", ex);
            }
        }

        // calculating the current journaling streak in days
        public async Task<int> GetCurrentStreakAsync()
        {
            try
            {
                await InitializeTableAsync();

                var entries = await _databaseService.Connection.Table<JournalEntry>()
                    .OrderByDescending(e => e.Date)
                    .ToListAsync();

                if (entries.Count == 0) return 0;

                // getting unique dates sorted descending
                var dates = entries
                    .Select(e => e.Date.Date)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .ToList();

                var today = DateTime.Now.Date;

                // streak starts from today if entry exists or yesterday if that has entry
                DateTime? anchor = dates.FirstOrDefault() == today ? today
                    : (dates.FirstOrDefault() == today.AddDays(-1) ? today.AddDays(-1) : null);

                if (anchor is null) return 0;

                // counting consecutive days going backwards
                var streak = 0;
                while (dates.Contains(anchor.Value.AddDays(-streak)))
                    streak++;

                return streak;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCurrentStreakAsync error: {ex}");
                throw new ApplicationException("GetCurrentStreakAsync failed", ex);
            }
        }
    }
}

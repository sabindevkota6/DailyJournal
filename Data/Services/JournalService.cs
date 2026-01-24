using DailyJournal.Core.Services;
using DailyJournal.Data.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DailyJournal.Data.Services
{
    public class JournalService : IJournalService
    {
        private readonly IDatabaseService _databaseService;

        public JournalService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        private async Task InitializeTableAsync()
        {
            await _databaseService.Connection.CreateTableAsync<JournalEntry>();
        }

        private static DateTime NormalizeDate(DateTime date) => date.ToUniversalTime().Date;

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
                throw new ApplicationException("GetEntryByIdAsync failed", ex);
            }
        }

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
                throw new ApplicationException("GetEntryByDateAsync failed", ex);
            }
        }

        public async Task<JournalEntry> CreateEntryAsync(
            DateTime date,
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
                var now = DateTime.UtcNow;

                var existing = await GetEntryByDateAsync(d);
                if (existing != null)
                    throw new InvalidOperationException($"An entry already exists for date {d:yyyy-MM-dd}.");

                var secondary = (secondaryMoods ?? Enumerable.Empty<Mood>())
                    .Distinct()
                    .Take(2)
                    .ToList();

                var normalizedTags = (tags ?? Enumerable.Empty<string>())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var entry = new JournalEntry
                {
                    Date = d,
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
                return entry;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("CreateEntryAsync failed", ex);
            }
        }

        public async Task<JournalEntry> UpdateEntryAsync(
            Guid id,
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

                var existing = await GetEntryByIdAsync(id);
                if (existing == null)
                    throw new KeyNotFoundException($"Journal entry with id '{id}' was not found.");

                var now = DateTime.UtcNow;

                var secondary = (secondaryMoods ?? Enumerable.Empty<Mood>())
                    .Distinct()
                    .Take(2)
                    .ToList();

                var normalizedTags = (tags ?? Enumerable.Empty<string>())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                existing.Content = content ?? string.Empty;
                existing.IsMarkdown = isMarkdown;
                existing.PrimaryMood = primaryMood;
                existing.SecondaryMoods = secondary;
                existing.Category = category;
                existing.Tags = normalizedTags;
                existing.UpdatedAt = now;

                await _databaseService.Connection.UpdateAsync(existing);
                return existing;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("UpdateEntryAsync failed", ex);
            }
        }

        // Backward compat wrapper.
        public async Task<JournalEntry> CreateOrUpdateEntryAsync(
            DateTime date,
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
                        return await CreateEntryAsync(d, content, isMarkdown, primaryMood, secondaryMoods, category, tags);
                    }
                    catch (SQLiteException)
                    {
                        // Unique Date index might race; fall back to update.
                        var again = await GetEntryByDateAsync(d);
                        if (again == null) throw;

                        return await UpdateEntryAsync(again.Id, content, isMarkdown, primaryMood, secondaryMoods, category, tags);
                    }
                }

                return await UpdateEntryAsync(existing.Id, content, isMarkdown, primaryMood, secondaryMoods, category, tags);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("CreateOrUpdateEntryAsync failed", ex);
            }
        }

        public async Task<bool> DeleteEntryAsync(DateTime date)
        {
            try
            {
                await InitializeTableAsync();
                var d = NormalizeDate(date);
                var existing = await GetEntryByDateAsync(d);
                if (existing == null) return false;
                await _databaseService.Connection.DeleteAsync(existing);
                return true;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("DeleteEntryAsync failed", ex);
            }
        }

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

                // SQLite-net has limited translation for complex conditions; use LINQ in-memory.
                var list = await _databaseService.Connection.Table<JournalEntry>().ToListAsync();
                var q = list.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var needle = query.Trim();
                    q = q.Where(e => (e.Content ?? string.Empty).Contains(needle, StringComparison.OrdinalIgnoreCase));
                }

                if (start.HasValue)
                {
                    var s = NormalizeDate(start.Value);
                    q = q.Where(e => e.Date >= s);
                }

                if (end.HasValue)
                {
                    var e = NormalizeDate(end.Value);
                    q = q.Where(x => x.Date <= e);
                }

                var moodSet = (moods ?? Enumerable.Empty<Mood>()).Distinct().ToHashSet();
                if (moodSet.Count > 0)
                {
                    q = q.Where(e => moodSet.Contains(e.PrimaryMood) || e.SecondaryMoods.Any(moodSet.Contains));
                }

                var tagSet = (tags ?? Enumerable.Empty<string>())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (tagSet.Count > 0)
                {
                    q = q.Where(e => e.Tags.Any(tagSet.Contains));
                }

                return q.OrderByDescending(e => e.Date).ToList();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("SearchAsync failed", ex);
            }
        }

        public async Task<int> GetCurrentStreakAsync()
        {
            try
            {
                await InitializeTableAsync();

                var entries = await _databaseService.Connection.Table<JournalEntry>()
                    .OrderByDescending(e => e.Date)
                    .ToListAsync();

                if (entries.Count == 0) return 0;

                var dates = entries
                    .Select(e => e.Date.Date)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .ToList();

                var today = DateTime.UtcNow.Date;

                // streak ends at today if present, otherwise yesterday if present
                DateTime? anchor = dates.FirstOrDefault() == today ? today
                    : (dates.FirstOrDefault() == today.AddDays(-1) ? today.AddDays(-1) : null);

                if (anchor is null) return 0;

                var streak = 0;
                while (dates.Contains(anchor.Value.AddDays(-streak)))
                    streak++;

                return streak;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("GetCurrentStreakAsync failed", ex);
            }
        }
    }
}

using DailyJournal.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DailyJournal.Core.Services
{
 public interface IJournalService
 {
 Task<JournalEntry?> GetEntryByIdAsync(Guid id);
 Task<JournalEntry?> GetEntryByDateAsync(DateTime date);

 Task<JournalEntry> CreateEntryAsync(DateTime date, string title, string content, bool isMarkdown, Mood primaryMood, IEnumerable<Mood>? secondaryMoods = null, Category? category = null, IEnumerable<string>? tags = null);
 Task<JournalEntry> UpdateEntryAsync(Guid id, string title, string content, bool isMarkdown, Mood primaryMood, IEnumerable<Mood>? secondaryMoods = null, Category? category = null, IEnumerable<string>? tags = null);

 Task<JournalEntry> CreateOrUpdateEntryAsync(DateTime date, string title, string content, bool isMarkdown, Mood primaryMood, IEnumerable<Mood>? secondaryMoods = null, Category? category = null, IEnumerable<string>? tags = null);

 Task<bool> DeleteEntryAsync(DateTime date);
 Task<bool> DeleteEntryByIdAsync(Guid id);
 Task<List<JournalEntry>> SearchAsync(string? query = null, DateTime? start = null, DateTime? end = null, IEnumerable<Mood>? moods = null, IEnumerable<string>? tags = null);
 Task<int> GetCurrentStreakAsync();
 }
}

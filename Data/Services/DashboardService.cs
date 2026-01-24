using DailyJournal.Core.Services;
using DailyJournal.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DailyJournal.Data.Services;

public class DashboardService : IDashboardService
{
 private readonly IDatabaseService _databaseService;

 public DashboardService(IDatabaseService databaseService)
 {
 _databaseService = databaseService;
 }

 private static DateTime NormalizeDate(DateTime date) => date.ToUniversalTime().Date;

 public async Task<DashboardAnalyticsResult> GetAnalyticsAsync(DateTime start, DateTime end, int topTags =10)
 {
 try
 {
 var s = NormalizeDate(start);
 var e = NormalizeDate(end);
 if (e < s) (s, e) = (e, s);

 // SQLite-net can struggle translating complex projections; load & use LINQ.
 var entries = (await _databaseService.Connection.Table<JournalEntry>().ToListAsync())
 .Where(x => x.Date >= s && x.Date <= e)
 .OrderBy(x => x.Date)
 .ToList();

 var total = entries.Count;

 var moodDistribution = BuildMoodDistribution(entries);
 var mostFrequentMood = entries
 .GroupBy(x => x.PrimaryMood)
 .OrderByDescending(g => g.Count())
 .Select(g => (Mood?)g.Key)
 .FirstOrDefault();

 var allDatesWithEntry = entries.Select(x => x.Date.Date).Distinct().ToHashSet();
 var missedDays = Enumerable.Range(0, (e - s).Days +1)
 .Select(i => s.AddDays(i))
 .Where(d => !allDatesWithEntry.Contains(d))
 .ToList();

 var currentStreak = CalculateCurrentStreak(allDatesWithEntry);
 var longestStreak = CalculateLongestStreak(allDatesWithEntry);

 var mostUsedTags = entries
 .SelectMany(x => x.Tags)
 .Where(t => !string.IsNullOrWhiteSpace(t))
 .Select(t => t.Trim())
 .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
 .OrderByDescending(g => g.Count())
 .ThenBy(g => g.Key)
 .Take(Math.Max(0, topTags))
 .Select(g => new TagCountItem(g.Key, g.Count()))
 .ToList();

 // Category breakdown (Work/Health/Travel etc.) isn't currently modeled.
 // Simple approach: treat first token before ':' as a "tag category" (e.g., "Work:Meeting").
 var tagBreakdown = entries
 .SelectMany(x => x.Tags)
 .Select(ParseTagCategory)
 .Where(c => c is not null)
 .Select(c => c!.Value)
 .GroupBy(c => c)
 .OrderByDescending(g => g.Count())
 .Select(g => new CategoryBreakdownItem(g.Key, g.Count(), Percentage(g.Count(), entries.Count ==0 ?0 : entries.Count)))
 .ToList();

 var wordCountTrends = entries
 .Select(x => new WordCountTrendItem(x.Date.Date, CountWords(x.Content)))
 .ToList();

 var avgWords = total ==0 ?0 : wordCountTrends.Average(x => x.WordCount);

 return new DashboardAnalyticsResult(
 Start: s,
 End: e,
 MoodDistribution: moodDistribution,
 MostFrequentMood: mostFrequentMood,
 CurrentStreak: currentStreak,
 LongestStreak: longestStreak,
 MissedDays: missedDays,
 MostUsedTags: mostUsedTags,
 TagBreakdown: tagBreakdown,
 WordCountTrends: wordCountTrends,
 AverageWordsPerEntry: avgWords);
 }
 catch (Exception ex)
 {
 throw new ApplicationException("GetAnalyticsAsync failed", ex);
 }
 }

 private static IReadOnlyList<MoodDistributionItem> BuildMoodDistribution(List<JournalEntry> entries)
 {
 if (entries.Count ==0)
 {
 return Enum.GetValues<Category>()
 .Select(c => new MoodDistributionItem(c,0,0))
 .ToList();
 }

 // Use Entry.Category when present; otherwise infer from the mood enum groups.
 static Category Infer(Mood mood) => mood switch
 {
 Mood.Happy or Mood.Excited or Mood.Relaxed or Mood.Grateful or Mood.Confident => Category.Positive,
 Mood.Calm or Mood.Thoughtful or Mood.Curious or Mood.Nostalgic or Mood.Bored => Category.Neutral,
 _ => Category.Negative
 };

 var categorized = entries
 .Select(e => e.Category ?? Infer(e.PrimaryMood))
 .ToList();

 return Enum.GetValues<Category>()
 .Select(c =>
 {
 var count = categorized.Count(x => x == c);
 return new MoodDistributionItem(c, count, Percentage(count, categorized.Count));
 })
 .ToList();
 }

 private static double Percentage(int part, int total)
 => total <=0 ?0 : Math.Round((double)part *100d / total,2);

 private static int CalculateCurrentStreak(HashSet<DateTime> datesWithEntry)
 {
 if (datesWithEntry.Count ==0) return 0;

 var today = DateTime.UtcNow.Date;
 var anchor = datesWithEntry.Contains(today) ? today
 : (datesWithEntry.Contains(today.AddDays(-1)) ? today.AddDays(-1) : (DateTime?)null);

 if (anchor is null) return 0;

 var streak =0;
 while (datesWithEntry.Contains(anchor.Value.AddDays(-streak)))
 streak++;

 return streak;
 }

 private static int CalculateLongestStreak(HashSet<DateTime> datesWithEntry)
 {
 if (datesWithEntry.Count ==0) return 0;

 var ordered = datesWithEntry.OrderBy(d => d).ToList();

 var best =1;
 var current =1;

 for (var i =1; i < ordered.Count; i++)
 {
 if (ordered[i] == ordered[i -1].AddDays(1))
 {
 current++;
 best = Math.Max(best, current);
 }
 else
 {
 current =1;
 }
 }

 return best;
 }

 private static int CountWords(string? text)
 {
 if (string.IsNullOrWhiteSpace(text)) return 0;

 // Simple word count: sequences of letters/digits
 return Regex.Matches(text, @"\b[\p{L}\p{N}]++\b").Count;
 }

 private static Category? ParseTagCategory(string? tag)
 {
 if (string.IsNullOrWhiteSpace(tag)) return null;
 var t = tag.Trim();

 // Map a few common categories; otherwise ignore.
 if (t.StartsWith("work", StringComparison.OrdinalIgnoreCase)) return Category.Neutral;
 if (t.StartsWith("health", StringComparison.OrdinalIgnoreCase)) return Category.Positive;
 if (t.StartsWith("travel", StringComparison.OrdinalIgnoreCase)) return Category.Positive;

 // Support format "Category:Something" -> infer category name to enum if matches.
 var idx = t.IndexOf(':');
 if (idx >0)
 {
 var head = t[..idx].Trim();
 if (Enum.TryParse<Category>(head, ignoreCase: true, out var c))
 return c;
 }

 return null;
 }
}

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

    private static DateTime NormalizeDate(DateTime date) => date.Date;

    public async Task<DashboardAnalyticsResult> GetAnalyticsAsync(DateTime start, DateTime end, int topTags = 10)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"DashboardService: GetAnalyticsAsync called with start={start:yyyy-MM-dd}, end={end:yyyy-MM-dd}");

            var s = NormalizeDate(start);
            var e = NormalizeDate(end);

            if (e < s)
            {
                (s, e) = (e, s);
            }

            System.Diagnostics.Debug.WriteLine($"DashboardService: Normalized dates s={s:yyyy-MM-dd}, e={e:yyyy-MM-dd}");

            var entries = (await _databaseService.Connection.Table<JournalEntry>().ToListAsync())
                .Where(x => x.Date >= s && x.Date <= e)
                .OrderBy(x => x.Date)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"DashboardService: Found {entries.Count} entries in date range");

            var total = entries.Count;
            var moodDistribution = BuildMoodDistribution(entries);

            System.Diagnostics.Debug.WriteLine($"DashboardService: Built mood distribution with {moodDistribution.Count} items");

            var mostFrequentMood = entries
                .GroupBy(x => x.PrimaryMood)
                .OrderByDescending(g => g.Count())
                .Select(g => (Mood?)g.Key)
                .FirstOrDefault();

            var allDatesWithEntry = entries.Select(x => x.Date.Date).Distinct().ToHashSet();
            var missedDays = Enumerable.Range(0, (e - s).Days + 1)
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

            System.Diagnostics.Debug.WriteLine($"DashboardService: Found {mostUsedTags.Count} most used tags");

            var tagBreakdown = BuildCategoryBreakdown(entries);

            System.Diagnostics.Debug.WriteLine($"DashboardService: Built category breakdown with {tagBreakdown.Count} items");

            var wordCountTrends = entries
                .Select(x => new WordCountTrendItem(x.Date.Date, CountWords(x.Content)))
                .ToList();

            var avgWords = total == 0 ? 0 : wordCountTrends.Average(x => x.WordCount);

            System.Diagnostics.Debug.WriteLine($"DashboardService: Completed successfully");

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
            System.Diagnostics.Debug.WriteLine($"DashboardService ERROR: {ex}");
            throw new ApplicationException("GetAnalyticsAsync failed", ex);
        }
    }

    private static IReadOnlyList<MoodDistributionItem> BuildMoodDistribution(List<JournalEntry> entries)
    {
        try
        {
            if (entries.Count == 0)
            {
                return Enum.GetValues<Category>()
                    .Select(c => new MoodDistributionItem(c, 0, 0))
                    .ToList();
            }

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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in BuildMoodDistribution: {ex.Message}");
            throw;
        }
    }

    private static IReadOnlyList<CategoryBreakdownItem> BuildCategoryBreakdown(List<JournalEntry> entries)
    {
        try
        {
            if (entries.Count == 0)
            {
                return Enum.GetValues<Category>()
                    .Select(c => new CategoryBreakdownItem(c, 0, 0))
                    .ToList();
            }

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
                    return new CategoryBreakdownItem(c, count, Percentage(count, categorized.Count));
                })
                .Where(item => item.Count > 0)
                .OrderByDescending(item => item.Count)
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in BuildCategoryBreakdown: {ex.Message}");
            throw;
        }
    }

    private static double Percentage(int part, int total)
    {
        return total <= 0 ? 0 : Math.Round((double)part * 100d / total, 2);
    }

    private static int CalculateCurrentStreak(HashSet<DateTime> datesWithEntry)
    {
        try
        {
            if (datesWithEntry.Count == 0)
            {
                return 0;
            }

            var today = DateTime.Now.Date;
            var anchor = datesWithEntry.Contains(today) ? today
                : (datesWithEntry.Contains(today.AddDays(-1)) ? today.AddDays(-1) : (DateTime?)null);

            if (anchor is null)
            {
                return 0;
            }

            var streak = 0;
            while (datesWithEntry.Contains(anchor.Value.AddDays(-streak)))
            {
                streak++;
            }

            return streak;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in CalculateCurrentStreak: {ex.Message}");
            return 0;
        }
    }

    private static int CalculateLongestStreak(HashSet<DateTime> datesWithEntry)
    {
        try
        {
            if (datesWithEntry.Count == 0)
            {
                return 0;
            }

            var ordered = datesWithEntry.OrderBy(d => d).ToList();
            var best = 1;
            var current = 1;

            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i] == ordered[i - 1].AddDays(1))
                {
                    current++;
                    best = Math.Max(best, current);
                }
                else
                {
                    current = 1;
                }
            }

            return best;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in CalculateLongestStreak: {ex.Message}");
            return 0;
        }
    }

    private static int CountWords(string? text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            return Regex.Matches(text, @"\b[\p{L}\p{N}]+\b").Count;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in CountWords: {ex.Message}");
            return 0;
        }
    }
}

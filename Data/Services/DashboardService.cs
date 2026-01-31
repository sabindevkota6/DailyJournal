using DailyJournal.Core.Services;
using DailyJournal.Data.Models;
using System.Text.RegularExpressions;

namespace DailyJournal.Data.Services;

// service class for generating dashboard analytics
public class DashboardService : IDashboardService
{
    // database service dependency
    private readonly IDatabaseService _databaseService;

    // constructor injecting the database service
    public DashboardService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    // normalizing a date to midnight
    private static DateTime NormalizeDate(DateTime date) => date.Date;

    // getting analytics data for a given date range
    public async Task<DashboardAnalyticsResult> GetAnalyticsAsync(DateTime start, DateTime end, int topTags = 10)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"DashboardService: GetAnalyticsAsync called with start={start:yyyy-MM-dd}, end={end:yyyy-MM-dd}");

            var s = NormalizeDate(start);
            var e = NormalizeDate(end);

            // swapping dates if end is before start
            if (e < s)
            {
                (s, e) = (e, s);
            }

            System.Diagnostics.Debug.WriteLine($"DashboardService: Normalized dates s={s:yyyy-MM-dd}, e={e:yyyy-MM-dd}");

            // fetching entries within the date range
            var entries = (await _databaseService.Connection.Table<JournalEntry>().ToListAsync())
                .Where(x => x.Date >= s && x.Date <= e)
                .OrderBy(x => x.Date)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"DashboardService: Found {entries.Count} entries in date range");

            var total = entries.Count;

            // building mood distribution statistics
            var moodDistribution = BuildMoodDistribution(entries);

            System.Diagnostics.Debug.WriteLine($"DashboardService: Built mood distribution with {moodDistribution.Count} items");

            // finding the most frequently used mood (from secondary moods since primary mood is just a category default)
            var allMoods = entries
                .SelectMany(x => x.SecondaryMoods)
                .ToList();
            var mostFrequentMood = allMoods.Any()
                ? allMoods
                .GroupBy(m => m)
                .OrderByDescending(g => g.Count())
                .Select(g => (Mood?)g.Key)
                .FirstOrDefault()
                : null;

            // calculating missed days (days without entries)
            var allDatesWithEntry = entries.Select(x => x.Date.Date).Distinct().ToHashSet();
            var missedDays = Enumerable.Range(0, (e - s).Days + 1)
                .Select(i => s.AddDays(i))
                .Where(d => !allDatesWithEntry.Contains(d))
                .ToList();

            // calculating streaks
            var currentStreak = CalculateCurrentStreak(allDatesWithEntry);
            var longestStreak = CalculateLongestStreak(allDatesWithEntry);

            // getting most used tags with counts
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

            // building category breakdown
            var tagBreakdown = BuildCategoryBreakdown(entries);

            System.Diagnostics.Debug.WriteLine($"DashboardService: Built category breakdown with {tagBreakdown.Count} items");

            // calculating word count trends
            var wordCountTrends = entries
                .Select(x => new WordCountTrendItem(x.Date.Date, CountWords(x.Content)))
                .ToList();

            // calculating average words per entry
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

    // building mood distribution showing count and percentage for each category
    private static IReadOnlyList<MoodDistributionItem> BuildMoodDistribution(List<JournalEntry> entries)
    {
        try
        {
            // returning empty distribution if no entries
            if (entries.Count == 0)
            {
                return Enum.GetValues<Category>()
                    .Select(c => new MoodDistributionItem(c, 0, 0))
                    .ToList();
            }

            // helper to infer category from mood if not set
            static Category Infer(Mood mood) => mood switch
            {
                Mood.Happy or Mood.Excited or Mood.Relaxed or Mood.Grateful or Mood.Confident => Category.Positive,
                Mood.Calm or Mood.Thoughtful or Mood.Curious or Mood.Nostalgic or Mood.Bored => Category.Neutral,
                _ => Category.Negative
            };

            // getting category for each entry
            var categorized = entries
                .Select(e => e.Category ?? Infer(e.PrimaryMood))
                .ToList();

            // building distribution for each category
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

    // building category breakdown with counts and percentages
    private static IReadOnlyList<CategoryBreakdownItem> BuildCategoryBreakdown(List<JournalEntry> entries)
    {
        try
        {
            // returning empty breakdown if no entries
            if (entries.Count == 0)
            {
                return Enum.GetValues<Category>()
                    .Select(c => new CategoryBreakdownItem(c, 0, 0))
                    .ToList();
            }

            // helper to infer category from mood if not set
            static Category Infer(Mood mood) => mood switch
            {
                Mood.Happy or Mood.Excited or Mood.Relaxed or Mood.Grateful or Mood.Confident => Category.Positive,
                Mood.Calm or Mood.Thoughtful or Mood.Curious or Mood.Nostalgic or Mood.Bored => Category.Neutral,
                _ => Category.Negative
            };

            // getting category for each entry
            var categorized = entries
                .Select(e => e.Category ?? Infer(e.PrimaryMood))
                .ToList();

            // building breakdown sorted by count
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

    // calculating percentage with rounding
    private static double Percentage(int part, int total)
    {
        return total <= 0 ? 0 : Math.Round(part * 100d / total, 2);
    }

    // calculating the current consecutive day streak
    private static int CalculateCurrentStreak(HashSet<DateTime> datesWithEntry)
    {
        try
        {
            if (datesWithEntry.Count == 0)
            {
                return 0;
            }

            var today = DateTime.Now.Date;

            // anchor is today if has entry or yesterday if that has entry
            var anchor = datesWithEntry.Contains(today) ? today
               : (datesWithEntry.Contains(today.AddDays(-1)) ? today.AddDays(-1) : (DateTime?)null);

            if (anchor is null)
            {
                return 0;
            }

            // counting consecutive days going backwards
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

    // calculating the longest consecutive day streak ever
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

            // iterating through sorted dates to find consecutive sequences
            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i] == ordered[i - 1].AddDays(1))
                {
                    // consecutive day found
                    current++;
                    best = Math.Max(best, current);
                }
                else
                {
                    // gap found so resetting current streak
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

    // counting words in text using regex
    private static int CountWords(string? text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            // matching word boundaries with letters and numbers
            return Regex.Matches(text, @"\b[\p{L}\p{N}]+\b").Count;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in CountWords: {ex.Message}");
            return 0;
        }
    }
}

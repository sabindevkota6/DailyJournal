using DailyJournal.Data.Models;

namespace DailyJournal.Core.Services;

// record for mood distribution data showing category counts and percentages
public record MoodDistributionItem(Category Category, int Count, double Percentage);

// record for tag usage statistics
public record TagCountItem(string Tag, int Count);

// record for category breakdown with counts and percentages
public record CategoryBreakdownItem(Category Category, int Count, double Percentage);

// record for tracking word count over time
public record WordCountTrendItem(DateTime Date, int WordCount);

// record containing all analytics data for the dashboard
public record DashboardAnalyticsResult(
    DateTime Start,
    DateTime End,
    IReadOnlyList<MoodDistributionItem> MoodDistribution,
    Mood? MostFrequentMood,
    int CurrentStreak,
    int LongestStreak,
    IReadOnlyList<DateTime> MissedDays,
    IReadOnlyList<TagCountItem> MostUsedTags,
    IReadOnlyList<CategoryBreakdownItem> TagBreakdown,
    IReadOnlyList<WordCountTrendItem> WordCountTrends,
    double AverageWordsPerEntry);

// interface for dashboard analytics operations
public interface IDashboardService
{
    // getting analytics data for a date range with optional limit on top tags
    Task<DashboardAnalyticsResult> GetAnalyticsAsync(DateTime start, DateTime end, int topTags = 10);
}

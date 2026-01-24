using DailyJournal.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DailyJournal.Core.Services;

public record MoodDistributionItem(Category Category, int Count, double Percentage);
public record TagCountItem(string Tag, int Count);
public record CategoryBreakdownItem(Category Category, int Count, double Percentage);
public record WordCountTrendItem(DateTime Date, int WordCount);

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

public interface IDashboardService
{
 Task<DashboardAnalyticsResult> GetAnalyticsAsync(DateTime start, DateTime end, int topTags =10);
}

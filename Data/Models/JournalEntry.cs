using SQLite;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DailyJournal.Data.Models
{
 public enum Category
 {
 Positive,
 Neutral,
 Negative
 }

 public enum Mood
 {
 // Positive
 Happy,
 Excited,
 Relaxed,
 Grateful,
 Confident,

 // Neutral
 Calm,
 Thoughtful,
 Curious,
 Nostalgic,
 Bored,

 // Negative
 Sad,
 Angry,
 Stressed,
 Lonely,
 Anxious
 }

 [Table("JournalEntries")]
 public class JournalEntry
 {
 [PrimaryKey]
 public Guid Id { get; set; } = Guid.NewGuid();

 // One entry per day (UTC date)
 [Indexed(Unique = true)]
 public DateTime Date { get; set; }

 public string Content { get; set; } = string.Empty;

 // Content mode: HTML (Quill) or Markdown
 public bool IsMarkdown { get; set; }

 // Required for analytics
 public Mood PrimaryMood { get; set; }

 public string SecondaryMoodsJson { get; set; } = "[]";

 public Category? Category { get; set; }

 public string TagsJson { get; set; } = "[]";

 // System-generated
 public DateTime CreatedAt { get; set; }
 public DateTime UpdatedAt { get; set; }

 [Ignore]
 public List<Mood> SecondaryMoods
 {
 get => JsonSerializer.Deserialize<List<Mood>>(SecondaryMoodsJson) ?? new();
 set => SecondaryMoodsJson = JsonSerializer.Serialize(value ?? new());
 }

 [Ignore]
 public List<string> Tags
 {
 get => JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new();
 set => TagsJson = JsonSerializer.Serialize(value ?? new());
 }
 }
}

using DailyJournal.Core.Services;
using DailyJournal.Data.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DailyJournal.Data.Services;

// Note: file name requested as "Pdfservices".
public class Pdfservices : IPdfService
{
 private readonly IDatabaseService _databaseService;

 public Pdfservices(IDatabaseService databaseService)
 {
 _databaseService = databaseService;
 }

 private static DateTime NormalizeDate(DateTime date) => date.Date;

 public async Task<PdfExportResult> ExportJournalsAsync(DateTime start, DateTime end)
 {
 try
 {
 var s = NormalizeDate(start);
 var e = NormalizeDate(end);
 if (e < s) (s, e) = (e, s);

 var entries = (await _databaseService.Connection.Table<JournalEntry>().ToListAsync())
 .Where(x => x.Date >= s && x.Date <= e)
 .OrderBy(x => x.Date)
 .ToList();

 var fileName = $"journals_{s:yyyyMMdd}_{e:yyyyMMdd}.pdf";
 var bytes = BuildPdf(entries, s, e);

 return new PdfExportResult(fileName, "application/pdf", bytes);
 }
 catch (Exception ex)
 {
 throw new ApplicationException("ExportJournalsAsync failed", ex);
 }
 }

 private static byte[] BuildPdf(System.Collections.Generic.IReadOnlyList<JournalEntry> entries, DateTime start, DateTime end)
 {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("Daily Journal Export").FontSize(18).SemiBold();
                    col.Item().Text($"Date range: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}").FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    if (entries.Count == 0)
                    {
                        col.Item().Text("No entries found in this date range.").Italic().FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                        return;
                    }

                    foreach (var e in entries)
                    {
                        col.Item().Element(x => EntryCard(x, e));
                        col.Item().PaddingVertical(6);
                    }
                });

                page.Footer()
                .AlignCenter()
                .DefaultTextStyle(x => x.FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2))
                .Text(t =>
                {
                    t.Span("Generated ");
                    t.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    t.Span(" • Page ");
                    t.CurrentPageNumber();
                    t.Span("/");
                    t.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void EntryCard(QuestPDF.Infrastructure.IContainer container, JournalEntry entry)
    {
        var tags = entry.Tags?.Any() == true ? string.Join(", ", entry.Tags) : "-";
        var secondary = entry.SecondaryMoods?.Any() == true ? string.Join(", ", entry.SecondaryMoods) : "-";

        container.Border(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(10).Column(col =>
        {
            // Date and Mood Row
            col.Item().Row(row =>
            {
                row.RelativeItem().Text(entry.Date.ToString("yyyy-MM-dd")).SemiBold().FontSize(10);
                row.ConstantItem(140).AlignRight().Text(entry.PrimaryMood.ToString()).FontColor(QuestPDF.Helpers.Colors.Blue.Darken2).FontSize(10);
            });

            // Title - NEW: Display the entry title prominently
            if (!string.IsNullOrWhiteSpace(entry.Title))
            {
                col.Item().PaddingTop(8).Text(entry.Title).FontSize(14).SemiBold().FontColor(QuestPDF.Helpers.Colors.Black);
            }

            // Metadata
            col.Item().PaddingTop(4).Text($"Category: {entry.Category?.ToString() ?? "-"}").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
            col.Item().Text($"Secondary moods: {secondary}").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
            col.Item().Text($"Tags: {tags}").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);

            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);

            // Content - Strip HTML tags from Quill content
            var plainTextContent = StripHtmlTags(entry.Content);
            var safeContent = string.IsNullOrWhiteSpace(plainTextContent) ? "(empty)" : plainTextContent.Trim();
            col.Item().PaddingTop(8).Text(safeContent).FontSize(11);
        });
    }

 private static string StripHtmlTags(string? html)
 {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Remove HTML tags
        var text = Regex.Replace(html, "<[^>]*>", " ");

        // Decode HTML entities
        text = System.Net.WebUtility.HtmlDecode(text);

        // Replace multiple spaces with single space
        text = Regex.Replace(text, @"\s+", " ");

        // Trim and return
        return text.Trim();
    }
}

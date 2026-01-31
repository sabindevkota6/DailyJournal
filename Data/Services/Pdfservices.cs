using DailyJournal.Core.Services;
using DailyJournal.Data.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Text.RegularExpressions;
using PdfColors = QuestPDF.Helpers.Colors;

namespace DailyJournal.Data.Services;

// service class for exporting journal entries to PDF
public class Pdfservices : IPdfService
{
    // database service dependency
    private readonly IDatabaseService _databaseService;

    // constructor injecting the database service
    public Pdfservices(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    // normalizing a date to midnight
    private static DateTime NormalizeDate(DateTime date) => date.Date;

    // exporting journal entries within a date range to PDF
    public async Task<PdfExportResult> ExportJournalsAsync(DateTime start, DateTime end)
    {
        try
        {
            var s = NormalizeDate(start);
            var e = NormalizeDate(end);

            // swapping dates if end is before start
            if (e < s)
            {
                (s, e) = (e, s);
            }

            // fetching entries within the date range
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

    // building the PDF document from journal entries
    private static byte[] BuildPdf(IReadOnlyList<JournalEntry> entries, DateTime start, DateTime end)
    {
        // setting QuestPDF license type
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                // setting page size and margins
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11));

                // building page header with title and date range
                page.Header().Column(col =>
                {
                    col.Item().Text("Daily Journal Export").FontSize(18).SemiBold();
                    col.Item().Text($"Date range: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}").FontColor(PdfColors.Grey.Darken2);
                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(PdfColors.Grey.Lighten2);
                });

                // building page content with entry cards
                page.Content().PaddingTop(10).Column(col =>
                {
                    if (entries.Count == 0)
                    {
                        col.Item().Text("No entries found in this date range.").Italic().FontColor(PdfColors.Grey.Darken1);
                        return;
                    }

                    foreach (var e in entries)
                    {
                        col.Item().Element(x => EntryCard(x, e));
                        col.Item().PaddingVertical(6);
                    }
                });

                // building page footer with generation info and page numbers
                page.Footer()
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(9).FontColor(PdfColors.Grey.Darken2))
                    .Text(t =>
                    {
                        t.Span("Generated ");
                        t.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                        t.Span(" - Page ");
                        t.CurrentPageNumber();
                        t.Span("/");
                        t.TotalPages();
                    });
            });
        }).GeneratePdf();
    }

    // rendering a single journal entry as a card in the PDF
    private static void EntryCard(QuestPDF.Infrastructure.IContainer container, JournalEntry entry)
    {
        var tags = entry.Tags?.Any() == true ? string.Join(", ", entry.Tags) : "-";
        var secondaryMoods = entry.SecondaryMoods?.Any() == true ? string.Join(", ", entry.SecondaryMoods) : "-";
        var wordCount = GetWordCount(entry.Content);

        container.Border(1).BorderColor(PdfColors.Grey.Lighten2).Padding(12).Column(col =>
        {
            // entry date and word count row
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(metaCol =>
                {
                    metaCol.Item().Text(entry.Date.ToString("dddd, MMMM dd, yyyy")).SemiBold().FontSize(12);
                    metaCol.Item().Text($"{entry.CreatedAt:hh:mm tt} • {wordCount} words").FontSize(9).FontColor(PdfColors.Grey.Darken1);
                });
            });

            // moods and tags row
            col.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Column(moodCol =>
                {
                    moodCol.Item().Text(text =>
                    {
                        text.Span("Moods: ").FontSize(9).FontColor(PdfColors.Grey.Darken2);
                        text.Span($"{entry.Category?.ToString() ?? "Neutral"} (Primary)").FontSize(9).Bold();
                        text.Span($", {entry.PrimaryMood}").FontSize(9);
                        if (entry.SecondaryMoods?.Any() == true)
                        {
                            text.Span($", {secondaryMoods}").FontSize(9);
                        }
                    });

                    if (entry.Tags?.Any() == true)
                    {
                        moodCol.Item().PaddingTop(4).Text(text =>
                        {
                            text.Span("Tags: ").FontSize(9).FontColor(PdfColors.Grey.Darken2);
                            text.Span(tags).FontSize(9);
                        });
                    }
                });
            });

            col.Item().PaddingTop(10).LineHorizontal(1).LineColor(PdfColors.Grey.Lighten2);

            // entry title and content
            col.Item().PaddingTop(10).Column(contentCol =>
            {
                if (!string.IsNullOrWhiteSpace(entry.Title))
                {
                    contentCol.Item().Text(entry.Title).FontSize(16).SemiBold().FontColor(PdfColors.Black);
                    contentCol.Item().PaddingTop(8);
                }
                RenderHtmlContent(contentCol, entry.Content);
            });

            col.Item().PaddingTop(10).LineHorizontal(1).LineColor(PdfColors.Grey.Lighten2);

            // timestamps row
            col.Item().PaddingTop(6).Column(timestampCol =>
            {
                timestampCol.Item().Text($"Created: {entry.CreatedAt:dddd, MMMM dd, yyyy 'at' hh:mm tt}").FontSize(8).FontColor(PdfColors.Grey.Darken1);
                timestampCol.Item().Text($"Last updated: {entry.UpdatedAt:dddd, MMMM dd, yyyy 'at' hh:mm tt}").FontSize(8).FontColor(PdfColors.Grey.Darken1);
            });
        });
    }

    // counting words in content after stripping HTML tags
    private static int GetWordCount(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        var text = Regex.Replace(content, "<.*?>", string.Empty);
        var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Length;
    }

    // rendering HTML content to PDF with fallback to plain text
    private static void RenderHtmlContent(ColumnDescriptor col, string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            col.Item().Text("(empty)").Italic().FontColor(PdfColors.Grey.Darken1);
            return;
        }

        try
        {
            var elements = ParseHtml(html);
            foreach (var element in elements)
            {
                RenderElement(col, element);
            }
        }
        catch
        {
            // falling back to plain text if HTML parsing fails
            var plainText = StripHtmlTags(html);
            col.Item().Text(string.IsNullOrWhiteSpace(plainText) ? "(empty)" : plainText);
        }
    }

    // parsing HTML string into a list of elements
    private static List<HtmlElement> ParseHtml(string html)
    {
        var elements = new List<HtmlElement>();
        html = html.Replace("\r\n", "").Replace("\n", "").Trim();

        // regex pattern to match block level HTML elements
        var blockPattern = @"<(p|h1|h2|h3|h4|ul|ol|li|br|div)([^>]*)>(.*?)</\1>|<(br)\s*/?>|<(p|div)([^>]*)/>|([^<]+)";
        var matches = Regex.Matches(html, blockPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            if (match.Groups[1].Success)
            {
                var tag = match.Groups[1].Value.ToLower();
                var content = match.Groups[3].Value;
                elements.Add(new HtmlElement { Tag = tag, Content = content });
            }
            else if (match.Groups[4].Success)
            {
                elements.Add(new HtmlElement { Tag = "br", Content = "" });
            }
            else if (match.Groups[7].Success)
            {
                var text = match.Groups[7].Value.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    elements.Add(new HtmlElement { Tag = "text", Content = text });
                }
            }
        }

        // if no elements found trying plain text
        if (elements.Count == 0)
        {
            var plainText = StripHtmlTags(html);
            if (!string.IsNullOrWhiteSpace(plainText))
            {
                elements.Add(new HtmlElement { Tag = "p", Content = plainText });
            }
        }

        return elements;
    }

    // rendering an HTML element to the PDF column
    private static void RenderElement(ColumnDescriptor col, HtmlElement element)
    {
        switch (element.Tag)
        {
            case "h1":
                col.Item().PaddingVertical(4).Text(text => RenderInlineContent(text, element.Content, 18, true));
                break;
            case "h2":
                col.Item().PaddingVertical(3).Text(text => RenderInlineContent(text, element.Content, 16, true));
                break;
            case "h3":
                col.Item().PaddingVertical(2).Text(text => RenderInlineContent(text, element.Content, 14, true));
                break;
            case "h4":
                col.Item().PaddingVertical(2).Text(text => RenderInlineContent(text, element.Content, 12, true));
                break;
            case "ul":
                RenderList(col, element.Content, false);
                break;
            case "ol":
                RenderList(col, element.Content, true);
                break;
            case "br":
                col.Item().PaddingVertical(2);
                break;
            case "p":
            case "div":
            case "text":
            default:
                var content = element.Content.Trim();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    col.Item().PaddingVertical(2).Text(text => RenderInlineContent(text, content, 11, false));
                }
                break;
        }
    }

    // rendering inline content with formatting (bold, italic, etc)
    private static void RenderInlineContent(TextDescriptor text, string content, float fontSize, bool isBold)
    {
        content = System.Net.WebUtility.HtmlDecode(content);

        // pattern to match inline formatting tags
        var pattern = @"<(strong|b|em|i|u|s|strike)(?:[^>]*)>(.*?)</\1>|<span\s+style=""([^""]*)"">([^<]*)</span>|([^<]+)|<[^>]*>";
        var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (matches.Count == 0)
        {
            var plain = StripHtmlTags(content);
            if (isBold)
                text.Span(plain).FontSize(fontSize).Bold();
            else
                text.Span(plain).FontSize(fontSize);
            return;
        }

        foreach (Match match in matches)
        {
            if (match.Groups[1].Success)
            {
                // handling formatting tags (strong, em, etc)
                var tag = match.Groups[1].Value.ToLower();
                var innerText = StripHtmlTags(match.Groups[2].Value);
                innerText = System.Net.WebUtility.HtmlDecode(innerText);

                if (string.IsNullOrEmpty(innerText)) continue;

                var span = text.Span(innerText).FontSize(fontSize);
                if (isBold) span = span.Bold();

                switch (tag)
                {
                    case "strong":
                    case "b":
                        span.Bold();
                        break;
                    case "em":
                    case "i":
                        span.Italic();
                        break;
                    case "u":
                        span.Underline();
                        break;
                    case "s":
                    case "strike":
                        span.Strikethrough();
                        break;
                }
            }
            else if (match.Groups[3].Success)
            {
                // handling styled spans
                var style = match.Groups[3].Value;
                var innerText = System.Net.WebUtility.HtmlDecode(match.Groups[4].Value);

                if (string.IsNullOrEmpty(innerText)) continue;

                var span = text.Span(innerText).FontSize(fontSize);
                if (isBold) span = span.Bold();
                ApplyStyleToSpan(span, style);
            }
            else if (match.Groups[5].Success)
            {
                // handling plain text
                var plainText = match.Groups[5].Value;
                if (!string.IsNullOrEmpty(plainText))
                {
                    var span = text.Span(plainText).FontSize(fontSize);
                    if (isBold) span.Bold();
                }
            }
        }
    }

    // applying inline CSS styles to a text span
    private static void ApplyStyleToSpan(TextSpanDescriptor span, string style)
    {
        // matching font color
        var colorMatch = Regex.Match(style, @"(?<!background-)color:\s*rgb\((\d+),\s*(\d+),\s*(\d+)\)", RegexOptions.IgnoreCase);
        if (colorMatch.Success)
        {
            var r = byte.Parse(colorMatch.Groups[1].Value);
            var g = byte.Parse(colorMatch.Groups[2].Value);
            var b = byte.Parse(colorMatch.Groups[3].Value);
            var hex = $"#{r:X2}{g:X2}{b:X2}";
            span.FontColor(hex);
        }

        // matching background color
        var bgMatch = Regex.Match(style, @"background-color:\s*rgb\((\d+),\s*(\d+),\s*(\d+)\)", RegexOptions.IgnoreCase);
        if (bgMatch.Success)
        {
            var r = byte.Parse(bgMatch.Groups[1].Value);
            var g = byte.Parse(bgMatch.Groups[2].Value);
            var b = byte.Parse(bgMatch.Groups[3].Value);
            var hex = $"#{r:X2}{g:X2}{b:X2}";
            span.BackgroundColor(hex);
        }
    }

    // rendering ordered or unordered lists
    private static void RenderList(ColumnDescriptor col, string content, bool ordered)
    {
        var liPattern = @"<li[^>]*>(.*?)</li>";
        var matches = Regex.Matches(content, liPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        int index = 1;
        foreach (Match match in matches)
        {
            var itemContent = match.Groups[1].Value;
            var prefix = ordered ? $"{index}. " : "• ";

            col.Item().PaddingLeft(15).PaddingVertical(1).Text(text =>
            {
                text.Span(prefix).FontSize(11);
                RenderInlineContent(text, itemContent, 11, false);
            });

            index++;
        }
    }

    // stripping all HTML tags from a string
    private static string StripHtmlTags(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var text = Regex.Replace(html, "<[^>]*>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    // helper class to represent a parsed HTML element
    private class HtmlElement
    {
        public string Tag { get; set; } = "";
        public string Content { get; set; } = "";
    }
}

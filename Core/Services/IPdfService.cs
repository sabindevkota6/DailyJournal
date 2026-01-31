namespace DailyJournal.Core.Services;

// record containing the PDF export result with file details
public record PdfExportResult(
    string FileName,
    string ContentType,
    byte[] Bytes);

// interface for PDF export operations
public interface IPdfService
{
    // exporting journal entries within a date range to PDF
    Task<PdfExportResult> ExportJournalsAsync(DateTime start, DateTime end);
}

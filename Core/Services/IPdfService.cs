using System;
using System.Threading.Tasks;

namespace DailyJournal.Core.Services;

public record PdfExportResult(
 string FileName,
 string ContentType,
 byte[] Bytes);

public interface IPdfService
{
 Task<PdfExportResult> ExportJournalsAsync(DateTime start, DateTime end);
}

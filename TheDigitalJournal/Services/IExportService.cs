using System;
using System.Threading.Tasks;

namespace TheDigitalJournal.Services;

public interface IExportService
{
    Task<string> ExportJournalToHtmlAsync(DateTime? start = null, DateTime? end = null);
    Task<string> ExportJournalToPdfAsync();
}

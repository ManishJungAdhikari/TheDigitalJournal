using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDigitalJournal.Models;
using TheDigitalJournal.Utils;

namespace TheDigitalJournal.Services;

public class ExportService : IExportService
{
    private readonly IJournalService _journalService;

    public ExportService(IJournalService journalService)
    {
        _journalService = journalService;
    }

    public async Task<string> ExportJournalToHtmlAsync(DateTime? start = null, DateTime? end = null)
    {
        var entries = await _journalService.GetEntriesAsync(1, 10000, startDate: start, endDate: end);
        var sb = new StringBuilder();
        sb.Append("<div class='journal-export'>");
        sb.Append("<h1>The Digital Journal - My Reflections</h1>");
        if (start.HasValue || end.HasValue)
        {
            sb.Append($"<p class='export-range'>Report Period: {(start.HasValue ? start.Value.ToShortDateString() : "Beginning")} to {(end.HasValue ? end.Value.ToShortDateString() : "Today")}</p>");
        }

        foreach (var entry in entries)
        {
            sb.Append("<div class='entry' style='margin-bottom: 30px; padding-bottom: 20px; border-bottom: 1px solid #eee;'>");
            sb.Append($"<div class='date' style='color: #059669; font-weight: bold;'>{entry.Date:dddd, MMMM dd, yyyy}");
            
            var mood = entry.Moods.FirstOrDefault();
            if (mood != null) sb.Append($" <span class='mood'>| {mood.Icon} {mood.Name}</span>");
            
            sb.Append("</div>");
            
            if (!string.IsNullOrEmpty(entry.Title))
                sb.Append($"<h2 class='title' style='margin: 10px 0;'>{entry.Title}</h2>");
                
            sb.Append($"<div class='content'>{DbConstants.MarkdownToHtml(entry.Content)}</div>");
            
            if (entry.Tags.Any())
                sb.Append($"<div class='tags' style='font-style: italic; color: #666; font-size: 0.9rem; margin-top: 10px;'>Tags: {string.Join(", ", entry.Tags.Select(t => t.Name))}</div>");
                
            sb.Append("</div>");
        }
        sb.Append("</div>");
        return sb.ToString();
    }

    public Task<string> ExportJournalToPdfAsync()
    {
        throw new NotSupportedException("Native PDF export is not supported on this platform. Please use the 'Compatible' export option.");
    }
}

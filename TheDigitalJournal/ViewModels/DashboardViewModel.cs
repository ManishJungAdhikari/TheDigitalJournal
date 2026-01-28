using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TheDigitalJournal.Models;
using TheDigitalJournal.Services;

namespace TheDigitalJournal.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IJournalService _journalService;

    [ObservableProperty]
    private int _currentStreak;

    [ObservableProperty]
    private int _longestStreak;

    [ObservableProperty]
    private int _totalEntries;

    [ObservableProperty]
    private string _mostFrequentMood = "N/A";

    [ObservableProperty]
    private List<JournalEntry> _recentEntries = new();

    public DashboardViewModel(IJournalService journalService)
    {
        _journalService = journalService;
    }

    public async Task InitializeAsync()
    {
        CurrentStreak = await _journalService.GetCurrentStreakAsync();
        LongestStreak = await _journalService.GetLongestStreakAsync();
        
        var allEntries = await _journalService.GetEntriesAsync(1, 1000);
        TotalEntries = allEntries.Count;
        RecentEntries = allEntries.Take(3).ToList();

        if (allEntries.Any())
        {
            var moodCounts = allEntries
                .SelectMany(e => e.Moods)
                .GroupBy(m => m.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefault();

            if (moodCounts != null)
            {
                MostFrequentMood = moodCounts.Name;
            }
        }
    }
}
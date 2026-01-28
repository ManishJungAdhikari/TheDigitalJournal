using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TheDigitalJournal.Models;
using TheDigitalJournal.Services;

namespace TheDigitalJournal.ViewModels;

public partial class AnalyticsViewModel : ObservableObject
{
    private readonly IJournalService _journalService;

    [ObservableProperty] private int _currentStreak;
    [ObservableProperty] private int _longestStreak;
    [ObservableProperty] private int _totalEntries;
    [ObservableProperty] private int _totalWords;
    [ObservableProperty] private int _avgWordsPerEntry;
    [ObservableProperty] private Dictionary<string, int> _moodDistribution = new();
    [ObservableProperty] private string _moodChartData = "";
    [ObservableProperty] private List<TagCount> _topTags = new();
    [ObservableProperty] private List<CategoryCount> _categoryDistribution = new();
    [ObservableProperty] private List<WordCountTrend> _wordCountTrends = new();
    [ObservableProperty] private List<DateTime> _missedDays = new();
    [ObservableProperty] private string _mostFrequentMood = "N/A";

    public AnalyticsViewModel(IJournalService journalService)
    {
        _journalService = journalService;
    }

    public async Task InitializeAsync(DateTime? start = null, DateTime? end = null)
    {
        CurrentStreak = await _journalService.GetCurrentStreakAsync();
        LongestStreak = await _journalService.GetLongestStreakAsync();
        
        // Fetch filtered entries for all analytics calculations
        var allEntries = await _journalService.GetEntriesAsync(1, 10000, startDate: start, endDate: end);
        TotalEntries = allEntries.Count;
        
        if (TotalEntries > 0)
        {
            TotalWords = allEntries.Sum(e => string.IsNullOrEmpty(e.Content) ? 0 : e.Content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length);
            AvgWordsPerEntry = TotalWords / TotalEntries;

            MoodDistribution = allEntries
                .SelectMany(e => e.Moods)
                .GroupBy(m => m.Name)
                .ToDictionary(g => g.Key, g => g.Count());

            var topMood = allEntries
                .SelectMany(e => e.Moods)
                .GroupBy(m => m.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => new { Name = g.Key, Icon = g.First().Icon })
                .FirstOrDefault();

            if (topMood != null) MostFrequentMood = $"{topMood.Icon} {topMood.Name}";

            if (MoodDistribution.Any())
            {
                var totalMoods = MoodDistribution.Values.Sum();
                var currentPercentage = 0.0;
                var gradientParts = new List<string>();
                var colors = new[] { "#10b981", "#667eea", "#ef4444", "#f59e0b", "#6b7280" };
                int colorIndex = 0;

                foreach (var mood in MoodDistribution)
                {
                    var percentage = (double)mood.Value / totalMoods * 100;
                    var nextPercentage = currentPercentage + percentage;
                    var color = colors[colorIndex % colors.Length];
                    gradientParts.Add($"{color} {currentPercentage:F1}% {nextPercentage:F1}%");
                    currentPercentage = nextPercentage;
                    colorIndex++;
                }
                MoodChartData = $"conic-gradient({string.Join(", ", gradientParts)})";
            }

            TopTags = allEntries
                .SelectMany(e => e.Tags)
                .GroupBy(t => t.Name)
                .Select(g => new TagCount { 
                    Name = g.Key, 
                    Count = g.Count(),
                    Percentage = (int)((double)g.Count() / TotalEntries * 100) 
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            CategoryDistribution = allEntries
                .Where(e => e.Category != null)
                .GroupBy(e => e.Category!.Name)
                .Select(g => new CategoryCount { 
                    Name = g.Key, 
                    Count = g.Count(), 
                    Percentage = (int)((double)g.Count() / TotalEntries * 100) 
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            WordCountTrends = allEntries
                .OrderByDescending(e => e.Date)
                .Take(7)
                .OrderBy(e => e.Date)
                .Select(e => new WordCountTrend { 
                    Date = e.Date.ToString("MMM dd"), 
                    Count = string.IsNullOrEmpty(e.Content) ? 0 : e.Content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length 
                })
                .ToList();
                
            var windowSize = (end ?? DateTime.Today).Subtract(start ?? DateTime.Today.AddDays(-90)).Days + 1;
            var lastDays = Enumerable.Range(0, Math.Min(90, windowSize))
                .Select(i => (end ?? DateTime.Today).AddDays(-i))
                .ToList();
            
            var entryDates = allEntries.Select(e => e.Date.Date).ToHashSet();
            MissedDays = lastDays.Where(d => !entryDates.Contains(d)).OrderByDescending(d => d).ToList();
        }
        else
        {
            // Reset stats if no entries in range
            TotalWords = 0; AvgWordsPerEntry = 0; MostFrequentMood = "N/A"; MoodChartData = "";
            TopTags = new(); CategoryDistribution = new(); WordCountTrends = new(); MissedDays = new();
        }
    }
}

public class TagCount { public string Name { get; set; } = ""; public int Count { get; set; } public int Percentage { get; set; } }
public class CategoryCount { public string Name { get; set; } = ""; public int Count { get; set; } public int Percentage { get; set; } }
public class WordCountTrend { public string Date { get; set; } = ""; public int Count { get; set; } }

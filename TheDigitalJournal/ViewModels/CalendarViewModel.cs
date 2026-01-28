using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TheDigitalJournal.Models;
using TheDigitalJournal.Services;

namespace TheDigitalJournal.ViewModels;

public partial class CalendarViewModel : ObservableObject
{
    private readonly IJournalService _journalService;

    [ObservableProperty]
    private DateTime _currentMonth = DateTime.Today;

    [ObservableProperty]
    private List<JournalEntry> _monthEntries = new();

    [ObservableProperty]
    private JournalEntry? _selectedEntry;

    public CalendarViewModel(IJournalService journalService)
    {
        _journalService = journalService;
    }

    public async Task InitializeAsync()
    {
        await LoadMonthEntriesAsync();
    }

    public async Task LoadMonthEntriesAsync()
    {
        MonthEntries = await _journalService.GetEntriesByMonthAsync(CurrentMonth.Year, CurrentMonth.Month);
    }

    public async Task ChangeMonthAsync(int delta)
    {
        CurrentMonth = CurrentMonth.AddMonths(delta);
        await LoadMonthEntriesAsync();
    }

    public void SelectEntry(DateTime date)
    {
        SelectedEntry = MonthEntries.FirstOrDefault(e => e.Date.Date == date.Date);
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TheDigitalJournal.Models;
using TheDigitalJournal.Services;

namespace TheDigitalJournal.ViewModels;

public partial class EntriesViewModel : ObservableObject
{
    private readonly IJournalService _journalService;
    private const int PageSize = 5;

    [ObservableProperty]
    private ObservableCollection<JournalEntry> _entries = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    private int _currentPage = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    private int _totalPages = 1;

    [ObservableProperty]
    private string? _searchTerm;

    [ObservableProperty]
    private List<int> _selectedMoodIds = new();

    [ObservableProperty]
    private int? _selectedCategoryId;

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private List<int> _selectedTagIds = new();

    [ObservableProperty]
    private List<Mood> _moods = new();

    [ObservableProperty]
    private List<Category> _categories = new();

    [ObservableProperty]
    private List<Tag> _tags = new();

    public EntriesViewModel(IJournalService journalService)
    {
        _journalService = journalService;
    }

    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasPreviousPage => CurrentPage > 1;

    public async Task ChangePageAsync(int delta)
    {
        var newPage = CurrentPage + delta;
        if (newPage >= 1 && newPage <= TotalPages)
        {
            CurrentPage = newPage;
            await LoadEntriesAsync();
        }
    }

    public async Task InitializeAsync()
    {
        Moods = await _journalService.GetMoodsAsync();
        Categories = await _journalService.GetCategoriesAsync();
        Tags = await _journalService.GetTagsAsync();
        await LoadEntriesAsync();
    }

    public async Task LoadEntriesAsync()
    {
        IsLoading = true;
        try
        {
            var totalCount = await _journalService.GetEntriesCountAsync(
                SearchTerm, SelectedMoodIds, SelectedCategoryId, StartDate, EndDate, SelectedTagIds);
            
            TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);
            if (TotalPages == 0) TotalPages = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            var fetchedEntries = await _journalService.GetEntriesAsync(
                CurrentPage, PageSize, SearchTerm, SelectedMoodIds, SelectedCategoryId, StartDate, EndDate, SelectedTagIds);
            
            Entries = new ObservableCollection<JournalEntry>(fetchedEntries);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task DeleteEntryAsync(int id)
    {
        await _journalService.DeleteEntryAsync(id);
        await LoadEntriesAsync();
    }

    public void ToggleTagFilter(int tagId)
    {
        if (SelectedTagIds.Contains(tagId))
            SelectedTagIds.Remove(tagId);
        else
            SelectedTagIds.Add(tagId);
    }

    public void ToggleMoodFilter(int moodId)
    {
        if (SelectedMoodIds.Contains(moodId))
            SelectedMoodIds.Remove(moodId);
        else
            SelectedMoodIds.Add(moodId);
    }
}
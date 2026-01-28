using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TheDigitalJournal.Models;
using TheDigitalJournal.Services;

namespace TheDigitalJournal.ViewModels;

public partial class JournalEditorViewModel : ObservableObject
{
    private readonly IJournalService _journalService;

    [ObservableProperty]
    private JournalEntry _entry = new();

    [ObservableProperty]
    private List<Mood> _moods = new();

    [ObservableProperty]
    private List<Category> _categories = new();

    [ObservableProperty]
    private List<Tag> _availableTags = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _entryExistsForToday;

    public JournalEditorViewModel(IJournalService journalService)
    {
        _journalService = journalService;
        ResetEntry();
    }

    public async Task InitializeAsync(int? id = null)
    {
        Moods = await _journalService.GetMoodsAsync();
        Categories = await _journalService.GetCategoriesAsync();
        AvailableTags = await _journalService.GetTagsAsync();
        
        var today = await _journalService.GetTodayEntryAsync();
        EntryExistsForToday = today != null;

        if (id.HasValue)
        {
            var existing = await _journalService.GetEntryByIdAsync(id.Value);
            if (existing != null) { 
                Entry = existing; 
                if (Entry.Moods == null) Entry.Moods = new List<Mood>();
                if (Entry.Tags == null) Entry.Tags = new List<Tag>();
                return; 
            }
        }

        if (today != null) Entry = today;
        else ResetEntry();
    }

    public void ToggleTag(Tag tag)
    {
        if (Entry.Tags == null) Entry.Tags = new List<Tag>();
        var existing = Entry.Tags.FirstOrDefault(t => t.Id == tag.Id);
        if (existing != null) Entry.Tags.Remove(existing);
        else Entry.Tags.Add(tag);
        OnPropertyChanged(nameof(Entry));
    }

    public async Task<Tag> CreateCustomTagAsync(string name)
    {
        var newTag = await _journalService.CreateTagAsync(name);
        AvailableTags.Add(newTag);
        return newTag;
    }

    public void SetPrimaryMood(Mood mood)
    {
        if (Entry.Moods == null) Entry.Moods = new List<Mood>();
        
        // Remove this mood from secondary list if it was there
        var existingIdx = Entry.Moods.FindIndex(m => m.Id == mood.Id);
        if (existingIdx > 0) Entry.Moods.RemoveAt(existingIdx);

        if (Entry.Moods.Count > 0) Entry.Moods[0] = mood;
        else Entry.Moods.Add(mood);
        
        OnPropertyChanged(nameof(Entry));
    }

    public void ToggleSecondaryMood(Mood mood)
    {
        if (Entry.Moods == null || Entry.Moods.Count == 0) return; // Must have primary first
        
        var existingIdx = Entry.Moods.FindIndex(m => m.Id == mood.Id);
        if (existingIdx == 0) return; // Cannot toggle primary as secondary

        if (existingIdx > 0)
        {
            Entry.Moods.RemoveAt(existingIdx);
        }
        else
        {
            if (Entry.Moods.Count >= 3) return; // 1 primary + 2 secondary
            Entry.Moods.Add(mood);
        }
        OnPropertyChanged(nameof(Entry));
    }

    public async Task SaveAsync()
    {
        try
        {
            StatusMessage = "Saving...";
            Entry = await _journalService.SaveEntryAsync(Entry);
            StatusMessage = "Saved successfully!";
            EntryExistsForToday = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    public void ResetEntry()
    {
        Entry = new JournalEntry 
        { 
            Date = DateTime.Today,
            Moods = new List<Mood>(),
            Tags = new List<Tag>()
        };
        StatusMessage = string.Empty;
    }
}
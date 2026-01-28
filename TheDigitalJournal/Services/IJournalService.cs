using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheDigitalJournal.Models;

namespace TheDigitalJournal.Services;

public interface IJournalService
{
    Task<JournalEntry?> GetTodayEntryAsync();
    Task<JournalEntry?> GetEntryByIdAsync(int id);
    Task<JournalEntry> SaveEntryAsync(JournalEntry entry);
    Task DeleteEntryAsync(int id);
    Task<List<JournalEntry>> GetEntriesAsync(int page, int pageSize, string? searchTerm = null, List<int>? moodIds = null, int? categoryId = null, DateTime? startDate = null, DateTime? endDate = null, List<int>? tagIds = null);
    Task<int> GetEntriesCountAsync(string? searchTerm = null, List<int>? moodIds = null, int? categoryId = null, DateTime? startDate = null, DateTime? endDate = null, List<int>? tagIds = null);
    Task<List<JournalEntry>> GetEntriesByMonthAsync(int year, int month);
    Task<List<Mood>> GetMoodsAsync();
    Task<List<Category>> GetCategoriesAsync();
    Task<List<Tag>> GetTagsAsync();
    Task<Tag> CreateTagAsync(string name);
    Task<int> GetCurrentStreakAsync();
    Task<int> GetLongestStreakAsync();
}

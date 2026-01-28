using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using TheDigitalJournal.Data;
using TheDigitalJournal.Models;

namespace TheDigitalJournal.Services;

public class JournalService : IJournalService
{
    private readonly JournalDbContext _db;

    public JournalService(JournalDbContext db)
    {
        _db = db;
    }

    public async Task<JournalEntry?> GetTodayEntryAsync()
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        using var connection = _db.CreateConnection();
        
        var sql = @"
            SELECT e.*, c.* 
            FROM JournalEntries e
            LEFT JOIN Categories c ON e.CategoryId = c.Id
            WHERE e.Date = @Today";

        var entries = await connection.QueryAsync<JournalEntry, Category, JournalEntry>(
            sql,
            (entry, category) =>
            {
                entry.Category = category;
                return entry;
            },
            new { Today = today });

        var entry = entries.FirstOrDefault();
        if (entry != null)
        {
            await LoadDetails(entry, connection);
        }

        return entry;
    }

    public async Task<JournalEntry?> GetEntryByIdAsync(int id)
    {
        using var connection = _db.CreateConnection();
        var sql = @"
            SELECT e.*, c.* 
            FROM JournalEntries e
            LEFT JOIN Categories c ON e.CategoryId = c.Id
            WHERE e.Id = @Id";

        var entries = await connection.QueryAsync<JournalEntry, Category, JournalEntry>(
            sql,
            (entry, category) =>
            {
                entry.Category = category;
                return entry;
            },
            new { Id = id });

        var entry = entries.FirstOrDefault();
        if (entry != null)
        {
            await LoadDetails(entry, connection);
        }

        return entry;
    }

    private async Task LoadDetails(JournalEntry entry, System.Data.IDbConnection connection)
    {
        var tagsSql = @"
            SELECT t.* FROM Tags t
            INNER JOIN JournalEntryTags jt ON t.Id = jt.TagId
            WHERE jt.JournalEntryId = @Id";
        entry.Tags = (await connection.QueryAsync<Tag>(tagsSql, new { Id = entry.Id })).ToList();

        var moodsSql = @"
            SELECT m.* FROM Moods m
            INNER JOIN JournalEntryMoods jm ON m.Id = jm.MoodId
            WHERE jm.JournalEntryId = @Id
            ORDER BY jm.IsPrimary DESC";
        entry.Moods = (await connection.QueryAsync<Mood>(moodsSql, new { Id = entry.Id })).ToList();
    }

    public async Task<JournalEntry> SaveEntryAsync(JournalEntry entry)
    {
        using var connection = _db.CreateConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            var dateStr = entry.Date.ToString("yyyy-MM-dd");
            
            if (entry.Id == 0)
            {
                var existing = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT Id FROM JournalEntries WHERE Date = @Date", 
                    new { Date = dateStr }, 
                    transaction);
                
                if (existing != 0)
                {
                    throw new InvalidOperationException("An entry for this date already exists.");
                }

                var sql = @"
                    INSERT INTO JournalEntries (Date, Title, Content, CategoryId, CreatedAt, UpdatedAt)
                    VALUES (@Date, @Title, @Content, @CategoryId, @CreatedAt, @UpdatedAt);
                    SELECT last_insert_rowid();";
                
                entry.CreatedAt = DateTime.Now;
                entry.UpdatedAt = DateTime.Now;

                entry.Id = await connection.ExecuteScalarAsync<int>(sql, new {
                    Date = dateStr,
                    entry.Title,
                    entry.Content,
                    entry.CategoryId,
                    CreatedAt = entry.CreatedAt.ToString("O"),
                    UpdatedAt = entry.UpdatedAt.ToString("O")
                }, transaction);
            }
            else
            {
                var sql = @"
                    UPDATE JournalEntries 
                    SET Title = @Title, Content = @Content, CategoryId = @CategoryId, UpdatedAt = @UpdatedAt
                    WHERE Id = @Id";
                
                entry.UpdatedAt = DateTime.Now;
                await connection.ExecuteAsync(sql, new {
                    entry.Title,
                    entry.Content,
                    entry.CategoryId,
                    UpdatedAt = entry.UpdatedAt.ToString("O"),
                    entry.Id
                }, transaction);

                await connection.ExecuteAsync("DELETE FROM JournalEntryTags WHERE JournalEntryId = @Id", new { entry.Id }, transaction);
                await connection.ExecuteAsync("DELETE FROM JournalEntryMoods WHERE JournalEntryId = @Id", new { entry.Id }, transaction);
            }

            if (entry.Tags != null && entry.Tags.Any())
            {
                foreach (var tag in entry.Tags)
                {
                    await connection.ExecuteAsync(
                        "INSERT INTO JournalEntryTags (JournalEntryId, TagId) VALUES (@EntryId, @TagId)",
                        new { EntryId = entry.Id, TagId = tag.Id }, transaction);
                }
            }

            if (entry.Moods != null && entry.Moods.Any())
            {
                for (int i = 0; i < entry.Moods.Count; i++)
                {
                    bool isPrimary = (i == 0);
                    await connection.ExecuteAsync(
                        "INSERT INTO JournalEntryMoods (JournalEntryId, MoodId, IsPrimary) VALUES (@EntryId, @MoodId, @IsPrimary)",
                        new { EntryId = entry.Id, MoodId = entry.Moods[i].Id, IsPrimary = isPrimary ? 1 : 0 }, transaction);
                }
            }

            transaction.Commit();
            return entry;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task DeleteEntryAsync(int id)
    {
        using var connection = _db.CreateConnection();
        await connection.ExecuteAsync("DELETE FROM JournalEntries WHERE Id = @Id", new { Id = id });
    }

    public async Task<List<JournalEntry>> GetEntriesAsync(int page, int pageSize, string? searchTerm = null, List<int>? moodIds = null, int? categoryId = null, DateTime? startDate = null, DateTime? endDate = null, List<int>? tagIds = null)
    {
        using var connection = _db.CreateConnection();
        var sql = @"
            SELECT e.*, c.* 
            FROM JournalEntries e
            LEFT JOIN Categories c ON e.CategoryId = c.Id
            WHERE 1=1";
        
        var parameters = new DynamicParameters();
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            sql += " AND (e.Title LIKE @Search OR e.Content LIKE @Search)";
            parameters.Add("Search", $"%{searchTerm}%");
        }
        if (moodIds != null && moodIds.Any())
        {
            sql += " AND EXISTS (SELECT 1 FROM JournalEntryMoods jm WHERE jm.JournalEntryId = e.Id AND jm.MoodId IN @MoodIds)";
            parameters.Add("MoodIds", moodIds);
        }
        if (categoryId.HasValue)
        {
            sql += " AND e.CategoryId = @CategoryId";
            parameters.Add("CategoryId", categoryId.Value);
        }
        if (startDate.HasValue)
        {
            sql += " AND e.Date >= @StartDate";
            parameters.Add("StartDate", startDate.Value.ToString("yyyy-MM-dd"));
        }
        if (endDate.HasValue)
        {
            sql += " AND e.Date <= @EndDate";
            parameters.Add("EndDate", endDate.Value.ToString("yyyy-MM-dd"));
        }
        if (tagIds != null && tagIds.Any())
        {
            sql += " AND EXISTS (SELECT 1 FROM JournalEntryTags jt WHERE jt.JournalEntryId = e.Id AND jt.TagId IN @TagIds)";
            parameters.Add("TagIds", tagIds);
        }

        sql += " ORDER BY e.Date DESC LIMIT @Limit OFFSET @Offset";
        parameters.Add("Limit", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        var entries = (await connection.QueryAsync<JournalEntry, Category, JournalEntry>(
            sql,
            (entry, category) =>
            {
                entry.Category = category;
                return entry;
            },
            parameters)).ToList();

        if (entries.Any())
        {
            var ids = entries.Select(e => e.Id).ToList();
            
            var tagsSql = "SELECT jt.JournalEntryId, t.* FROM Tags t INNER JOIN JournalEntryTags jt ON t.Id = jt.TagId WHERE jt.JournalEntryId IN @Ids";
            var allTags = (await connection.QueryAsync<dynamic>(tagsSql, new { Ids = ids })).ToList();

            var moodsSql = "SELECT jm.JournalEntryId, m.* FROM Moods m INNER JOIN JournalEntryMoods jm ON m.Id = jm.MoodId WHERE jm.JournalEntryId IN @Ids ORDER BY jm.IsPrimary DESC";
            var allMoods = (await connection.QueryAsync<dynamic>(moodsSql, new { Ids = ids })).ToList();

            foreach (var entry in entries)
            {
                entry.Tags = allTags.Where(t => (long)t.JournalEntryId == entry.Id)
                    .Select(t => new Tag { Id = (int)(long)t.Id, Name = (string)t.Name }).ToList();
                entry.Moods = allMoods.Where(m => (long)m.JournalEntryId == entry.Id)
                    .Select(m => new Mood { Id = (int)(long)m.Id, Name = (string)m.Name, Icon = (string)m.Icon }).ToList();
            }
        }

        return entries;
    }

    public async Task<int> GetEntriesCountAsync(string? searchTerm = null, List<int>? moodIds = null, int? categoryId = null, DateTime? startDate = null, DateTime? endDate = null, List<int>? tagIds = null)
    {
        using var connection = _db.CreateConnection();
        var sql = "SELECT COUNT(*) FROM JournalEntries e WHERE 1=1";
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            sql += " AND (e.Title LIKE @Search OR e.Content LIKE @Search)";
            parameters.Add("Search", $"%{searchTerm}%");
        }
        if (moodIds != null && moodIds.Any())
        {
            sql += " AND EXISTS (SELECT 1 FROM JournalEntryMoods jm WHERE jm.JournalEntryId = e.Id AND jm.MoodId IN @MoodIds)";
            parameters.Add("MoodIds", moodIds);
        }
        if (categoryId.HasValue)
        {
            sql += " AND e.CategoryId = @CategoryId";
            parameters.Add("CategoryId", categoryId.Value);
        }
        if (startDate.HasValue)
        {
            sql += " AND e.Date >= @StartDate";
            parameters.Add("StartDate", startDate.Value.ToString("yyyy-MM-dd"));
        }
        if (endDate.HasValue)
        {
            sql += " AND e.Date <= @EndDate";
            parameters.Add("EndDate", endDate.Value.ToString("yyyy-MM-dd"));
        }
        if (tagIds != null && tagIds.Any())
        {
            sql += " AND EXISTS (SELECT 1 FROM JournalEntryTags jt WHERE jt.JournalEntryId = e.Id AND jt.TagId IN @TagIds)";
            parameters.Add("TagIds", tagIds);
        }

        return await connection.ExecuteScalarAsync<int>(sql, parameters);
    }

    public async Task<List<JournalEntry>> GetEntriesByMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1).ToString("yyyy-MM-dd");
        var end = new DateTime(year, month, 1).AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd");

        using var connection = _db.CreateConnection();
        var sql = @"
            SELECT e.*, c.* 
            FROM JournalEntries e
            LEFT JOIN Categories c ON e.CategoryId = c.Id
            WHERE e.Date >= @Start AND e.Date <= @End
            ORDER BY e.Date ASC";

        var entries = (await connection.QueryAsync<JournalEntry, Category, JournalEntry>(
            sql,
            (entry, category) =>
            {
                entry.Category = category;
                return entry;
            },
            new { Start = start, End = end })).ToList();

        if (entries.Any())
        {
            var ids = entries.Select(e => e.Id).ToList();
            var tagsSql = "SELECT jt.JournalEntryId, t.* FROM Tags t INNER JOIN JournalEntryTags jt ON t.Id = jt.TagId WHERE jt.JournalEntryId IN @Ids";
            var allTags = (await connection.QueryAsync<dynamic>(tagsSql, new { Ids = ids })).ToList();

            var moodsSql = "SELECT jm.JournalEntryId, m.* FROM Moods m INNER JOIN JournalEntryMoods jm ON m.Id = jm.MoodId WHERE jm.JournalEntryId IN @Ids ORDER BY jm.IsPrimary DESC";
            var allMoods = (await connection.QueryAsync<dynamic>(moodsSql, new { Ids = ids })).ToList();

            foreach (var entry in entries)
            {
                entry.Tags = allTags.Where(t => (long)t.JournalEntryId == entry.Id)
                    .Select(t => new Tag { Id = (int)(long)t.Id, Name = (string)t.Name }).ToList();
                entry.Moods = allMoods.Where(m => (long)m.JournalEntryId == entry.Id)
                    .Select(m => new Mood { Id = (int)(long)m.Id, Name = (string)m.Name, Icon = (string)m.Icon }).ToList();
            }
        }
        return entries;
    }

    public async Task<List<Mood>> GetMoodsAsync()
    {
        using var connection = _db.CreateConnection();
        return (await connection.QueryAsync<Mood>("SELECT * FROM Moods")).ToList();
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        using var connection = _db.CreateConnection();
        return (await connection.QueryAsync<Category>("SELECT * FROM Categories")).ToList();
    }

    public async Task<List<Tag>> GetTagsAsync()
    {
        using var connection = _db.CreateConnection();
        return (await connection.QueryAsync<Tag>("SELECT * FROM Tags")).ToList();
    }

    public async Task<Tag> CreateTagAsync(string name)
    {
        using var connection = _db.CreateConnection();
        var sql = "INSERT INTO Tags (Name) VALUES (@Name); SELECT last_insert_rowid();";
        var id = await connection.ExecuteScalarAsync<int>(sql, new { Name = name });
        return new Tag { Id = id, Name = name };
    }

    public async Task<int> GetCurrentStreakAsync()
    {
        using var connection = _db.CreateConnection();
        var dates = (await connection.QueryAsync<string>(
            "SELECT Date FROM JournalEntries ORDER BY Date DESC")).ToList();

        if (!dates.Any()) return 0;

        var today = DateTime.Today;
        var lastEntryDate = DateTime.Parse(dates.First());

        if (lastEntryDate < today.AddDays(-1)) return 0;

        int streak = 0;
        var currentDate = lastEntryDate;

        foreach (var dateStr in dates)
        {
            var date = DateTime.Parse(dateStr);
            if (date == currentDate)
            {
                streak++;
                currentDate = currentDate.AddDays(-1);
            }
            else if (date < currentDate)
            {
                break;
            }
        }

        return streak;
    }

    public async Task<int> GetLongestStreakAsync()
    {
        using var connection = _db.CreateConnection();
        var dates = (await connection.QueryAsync<string>(
            "SELECT DISTINCT Date FROM JournalEntries ORDER BY Date DESC")).ToList();

        if (!dates.Any()) return 0;

        int maxStreak = 0;
        int currentStreak = 0;
        DateTime? expectedDate = null;

        foreach (var dateStr in dates)
        {
            var date = DateTime.Parse(dateStr);
            if (expectedDate == null || date == expectedDate)
            {
                currentStreak++;
                expectedDate = date.AddDays(-1);
            }
            else
            {
                maxStreak = Math.Max(maxStreak, currentStreak);
                currentStreak = 1;
                expectedDate = date.AddDays(-1);
            }
        }

        return Math.Max(maxStreak, currentStreak);
    }
}
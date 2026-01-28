using System;
using System.Data;
using System.Linq;
using Microsoft.Data.Sqlite;
using Dapper;
using TheDigitalJournal.Utils;

namespace TheDigitalJournal.Data;

public class JournalDbContext
{
    private readonly string _connectionString;

    public JournalDbContext()
    {
        _connectionString = $"Data Source={DbConstants.DatabasePath}";
        InitializeDatabase();
    }

    public IDbConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        connection.Execute("PRAGMA foreign_keys = ON;");
        return connection;
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        connection.Execute("PRAGMA foreign_keys = ON;");

        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Users (Id INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT NOT NULL, PasswordHash TEXT NOT NULL, PasswordSalt TEXT NOT NULL, CreatedAt TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Categories (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, ColorHex TEXT);
            CREATE TABLE IF NOT EXISTS Moods (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Icon TEXT, IsPrimary INTEGER NOT NULL DEFAULT 0, Type TEXT NOT NULL DEFAULT 'Neutral');
            CREATE TABLE IF NOT EXISTS Tags (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE);
            CREATE TABLE IF NOT EXISTS JournalEntries (Id INTEGER PRIMARY KEY AUTOINCREMENT, Date TEXT NOT NULL UNIQUE, Title TEXT, Content TEXT, CategoryId INTEGER, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL, FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE SET NULL);
            CREATE TABLE IF NOT EXISTS JournalEntryTags (JournalEntryId INTEGER NOT NULL, TagId INTEGER NOT NULL, PRIMARY KEY (JournalEntryId, TagId), FOREIGN KEY (JournalEntryId) REFERENCES JournalEntries(Id) ON DELETE CASCADE, FOREIGN KEY (TagId) REFERENCES Tags(Id) ON DELETE CASCADE);
            CREATE TABLE IF NOT EXISTS JournalEntryMoods (JournalEntryId INTEGER NOT NULL, MoodId INTEGER NOT NULL, IsPrimary INTEGER NOT NULL DEFAULT 0, PRIMARY KEY (JournalEntryId, MoodId), FOREIGN KEY (JournalEntryId) REFERENCES JournalEntries(Id) ON DELETE CASCADE, FOREIGN KEY (MoodId) REFERENCES Moods(Id) ON DELETE CASCADE);
        ");
            
        SeedInitialData(connection);
    }

    private void SeedInitialData(IDbConnection connection)
    {
        if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Moods") == 0)
        {
            connection.Execute("INSERT INTO Moods (Name, Icon, IsPrimary, Type) VALUES ('Happy', '😊', 1, 'Positive'), ('Excited', '🤩', 0, 'Positive'), ('Relaxed', '😌', 0, 'Positive'), ('Grateful', '🙏', 0, 'Positive'), ('Confident', '😎', 0, 'Positive')");
            connection.Execute("INSERT INTO Moods (Name, Icon, IsPrimary, Type) VALUES ('Calm', '🧘', 1, 'Neutral'), ('Thoughtful', '🤔', 0, 'Neutral'), ('Curious', '🧐', 0, 'Neutral'), ('Nostalgic', '📻', 0, 'Neutral'), ('Bored', '😴', 0, 'Neutral'), ('Neutral', '😐', 1, 'Neutral')");
            connection.Execute("INSERT INTO Moods (Name, Icon, IsPrimary, Type) VALUES ('Sad', '😔', 1, 'Negative'), ('Angry', '😠', 0, 'Negative'), ('Stressed', '😫', 0, 'Negative'), ('Lonely', '🥺', 0, 'Negative'), ('Anxious', '😰', 0, 'Negative')");
        }

        if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Categories") == 0)
        {
            connection.Execute("INSERT INTO Categories (Name, ColorHex) VALUES ('Personal', '#FF5733'), ('Work', '#33FF57'), ('Health', '#3357FF'), ('Travel', '#F333FF')");
        }

        if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Tags") == 0)
        {
            var reqTags = new[] { "Work", "Family", "Health", "Fitness", "Hobbies", "Travel", "Planning", "Reflection" };
            foreach (var t in reqTags) connection.Execute("INSERT OR IGNORE INTO Tags (Name) VALUES (@N)", new { N = t });
        }

        // Seed entries if low count
        if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM JournalEntries") < 5)
        {
            var random = new Random();
            var moods = connection.Query<dynamic>("SELECT Id FROM Moods").ToList();
            var cats = connection.Query<dynamic>("SELECT Id FROM Categories").ToList();
            var tags = connection.Query<dynamic>("SELECT Id FROM Tags").ToList();

            for (int i = 15; i >= 1; i--)
            {
                var dt = DateTime.Today.AddDays(-i);
                var dateStr = dt.ToString("yyyy-MM-dd");
                
                var exists = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM JournalEntries WHERE Date = @D", new { D = dateStr });
                if (exists > 0) continue;

                var entryId = connection.ExecuteScalar<int>(@"
                    INSERT INTO JournalEntries (Date, Title, Content, CategoryId, CreatedAt, UpdatedAt)
                    VALUES (@Date, @Title, @Content, @CategoryId, @CreatedAt, @UpdatedAt);
                    SELECT last_insert_rowid();",
                    new {
                        Date = dateStr,
                        Title = $"Historical Reflection - Day {16-i}",
                        Content = $"This is a test entry for {dt:D}. I spent the day working on the Digital Journal app. The results are **amazing** and the UI is looking *very professional*.",
                        CategoryId = cats[random.Next(cats.Count)].Id,
                        CreatedAt = DateTime.UtcNow.ToString("O"),
                        UpdatedAt = DateTime.UtcNow.ToString("O")
                    });

                connection.Execute("INSERT INTO JournalEntryMoods (JournalEntryId, MoodId, IsPrimary) VALUES (@E, @M, 1)", 
                    new { E = entryId, M = moods[random.Next(moods.Count)].Id });
                
                connection.Execute("INSERT INTO JournalEntryMoods (JournalEntryId, MoodId, IsPrimary) VALUES (@E, @M, 0)", 
                    new { E = entryId, M = moods[random.Next(moods.Count)].Id });

                connection.Execute("INSERT INTO JournalEntryTags (JournalEntryId, TagId) VALUES (@E, @T)", 
                    new { E = entryId, T = tags[random.Next(tags.Count)].Id });
            }
        }
    }
}

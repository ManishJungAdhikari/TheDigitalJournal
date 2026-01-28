using System.ComponentModel.DataAnnotations.Schema;

namespace TheDigitalJournal.Models;

public class JournalEntryMood
{
    public int JournalEntryId { get; set; }
    [ForeignKey("JournalEntryId")]
    public JournalEntry JournalEntry { get; set; } = null!;
    
    public int MoodId { get; set; }
    [ForeignKey("MoodId")]
    public Mood Mood { get; set; } = null!;
    
    public bool IsPrimary { get; set; }
}

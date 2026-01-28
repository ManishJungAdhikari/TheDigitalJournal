using System.ComponentModel.DataAnnotations.Schema;

namespace TheDigitalJournal.Models;

public class JournalEntryTag
{
    public int JournalEntryId { get; set; }
    [ForeignKey("JournalEntryId")]
    public JournalEntry JournalEntry { get; set; } = null!;
    
    public int TagId { get; set; }
    [ForeignKey("TagId")]
    public Tag Tag { get; set; } = null!;
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TheDigitalJournal.Models;

public class JournalEntry
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    
    public List<Tag> Tags { get; set; } = new List<Tag>();
    public List<Mood> Moods { get; set; } = new List<Mood>();
}

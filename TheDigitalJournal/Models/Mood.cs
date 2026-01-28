namespace TheDigitalJournal.Models;

public class Mood
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "😐";
    public bool IsPrimary { get; set; } = true;
}
using System.ComponentModel.DataAnnotations;

namespace TheDigitalJournal.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using TheDigitalJournal.Data;
using TheDigitalJournal.Models;

namespace TheDigitalJournal.Services;

public class SecurityService : ISecurityService
{
    private readonly JournalDbContext _db;
    private bool _isAuthenticated;

    public SecurityService(JournalDbContext db)
    {
        _db = db;
    }

    public bool IsAuthenticated => _isAuthenticated;

    public async Task<bool> IsPinSetAsync()
    {
        using var connection = _db.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users");
        return count > 0;
    }

    public async Task<bool> SetPinAsync(string pin)
    {
        if (await IsPinSetAsync()) return false;

        var salt = GenerateSalt();
        var hash = HashPassword(pin, salt);

        using var connection = _db.CreateConnection();
        await connection.ExecuteAsync(
            "INSERT INTO Users (Username, PasswordHash, PasswordSalt, CreatedAt) VALUES (@Username, @Hash, @Salt, @CreatedAt)",
            new { Username = "Owner", Hash = hash, Salt = salt, CreatedAt = DateTime.UtcNow.ToString("O") });

        _isAuthenticated = true;
        return true;
    }

    public async Task<bool> ChangePinAsync(string oldPin, string newPin)
    {
        using var connection = _db.CreateConnection();
        var user = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM Users LIMIT 1");
        if (user == null) return false;

        if (VerifyPassword(oldPin, user.PasswordHash, user.PasswordSalt))
        {
            var newSalt = GenerateSalt();
            var newHash = HashPassword(newPin, newSalt);
            await connection.ExecuteAsync(
                "UPDATE Users SET PasswordHash = @Hash, PasswordSalt = @Salt WHERE Id = @Id",
                new { Hash = newHash, Salt = newSalt, Id = user.Id });
            return true;
        }

        return false;
    }

    public async Task<bool> AuthenticateAsync(string pin)
    {
        using var connection = _db.CreateConnection();
        var user = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM Users LIMIT 1");
        if (user == null) return false;

        if (VerifyPassword(pin, user.PasswordHash, user.PasswordSalt))
        {
            _isAuthenticated = true;
            return true;
        }

        return false;
    }

    public void Logout()
    {
        _isAuthenticated = false;
    }

    private string GenerateSalt()
    {
        var buffer = new byte[16];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer);
    }

    private string HashPassword(string password, string salt)
    {
        using var sha256 = SHA256.Create();
        var saltedPassword = password + salt;
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        return Convert.ToBase64String(bytes);
    }

    private bool VerifyPassword(string password, string hash, string salt)
    {
        var newHash = HashPassword(password, salt);
        return newHash == hash;
    }
}

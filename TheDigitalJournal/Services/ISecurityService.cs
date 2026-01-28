using System.Threading.Tasks;

namespace TheDigitalJournal.Services;

public interface ISecurityService
{
    bool IsAuthenticated { get; }
    Task<bool> IsPinSetAsync();
    Task<bool> SetPinAsync(string pin);
    Task<bool> ChangePinAsync(string oldPin, string newPin);
    Task<bool> AuthenticateAsync(string pin);
    void Logout();
}
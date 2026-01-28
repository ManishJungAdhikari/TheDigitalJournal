using System;

namespace TheDigitalJournal.Services;

public interface IThemeService
{
    bool IsDarkMode { get; }
    event Action OnThemeChanged;
    void ToggleTheme();
}

public class ThemeService : IThemeService
{
    private bool _isDarkMode;
    public bool IsDarkMode => _isDarkMode;
    public event Action? OnThemeChanged;

    public ThemeService()
    {
        _isDarkMode = false;
    }

    public void ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;
        OnThemeChanged?.Invoke();
    }
}
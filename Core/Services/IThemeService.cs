namespace DailyJournal.Core.Services;

// enum representing the available theme modes
public enum AppThemeMode
{
    System = 0,  // following system preference
    Light = 1,   // always light theme
    Dark = 2     // always dark theme
}

// interface for theme management
public interface IThemeService
{
    // getting the current theme mode setting
    AppThemeMode Mode { get; }

    // returning true if dark mode is currently active
    bool IsDarkMode { get; }

    // event fired when theme changes
    event Action? Changed;

    // initializing the theme service and loading saved preference
    Task InitializeAsync();

    // setting and saving the theme mode
    Task SetModeAsync(AppThemeMode mode);
}

using DailyJournal.Core.Services;

namespace DailyJournal.Data.Services;

// service class for managing application theme (light/dark mode)
public class ThemeService : IThemeService
{
    // key used to store theme preference
    private const string ThemeKey = "theme.mode";

    // current theme mode setting
    public AppThemeMode Mode { get; private set; } = AppThemeMode.System;

    // returning true if dark mode is currently active
    public bool IsDarkMode => Mode switch
    {
        AppThemeMode.Dark => true,
        AppThemeMode.Light => false,
        _ => Application.Current?.RequestedTheme == AppTheme.Dark  // following system preference
    };

    // event fired when theme changes
    public event Action? Changed;

    // initializing theme from saved preferences
    public Task InitializeAsync()
    {
        try
        {
            // loading saved theme preference or defaulting to System
            var stored = Preferences.Default.Get(ThemeKey, (int)AppThemeMode.System);
            Mode = Enum.IsDefined(typeof(AppThemeMode), stored) ? (AppThemeMode)stored : AppThemeMode.System;

            System.Diagnostics.Debug.WriteLine($"Theme initialized to mode: {Mode}");

            // applying theme and notifying listeners
            ApplyModeToMaui();
            Changed?.Invoke();

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing theme: {ex.Message}");
            Mode = AppThemeMode.System;
            return Task.CompletedTask;
        }
    }

    // setting and saving a new theme mode
    public Task SetModeAsync(AppThemeMode mode)
    {
        try
        {
            Mode = mode;
            Preferences.Default.Set(ThemeKey, (int)mode);

            System.Diagnostics.Debug.WriteLine($"Theme changed to mode: {Mode}");

            // applying theme and notifying listeners
            ApplyModeToMaui();
            Changed?.Invoke();

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting theme mode: {ex.Message}");
            throw new ApplicationException("Failed to set theme mode", ex);
        }
    }

    // applying the theme mode to the MAUI application
    private void ApplyModeToMaui()
    {
        try
        {
            if (Application.Current is null)
            {
                System.Diagnostics.Debug.WriteLine("Application.Current is null, cannot apply theme");
                return;
            }

            // mapping our theme mode to MAUI AppTheme
            Application.Current.UserAppTheme = Mode switch
            {
                AppThemeMode.Light => AppTheme.Light,
                AppThemeMode.Dark => AppTheme.Dark,
                _ => AppTheme.Unspecified  // letting system decide
            };

            System.Diagnostics.Debug.WriteLine($"Applied theme to MAUI: {Application.Current.UserAppTheme}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error applying theme to MAUI: {ex.Message}");
        }
    }
}

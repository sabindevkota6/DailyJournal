using DailyJournal.Core.Services;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;

namespace DailyJournal.Data.Services;

public class ThemeService : IThemeService
{
    private const string ThemeKey = "theme.mode";

    public AppThemeMode Mode { get; private set; } = AppThemeMode.System;

    public bool IsDarkMode => Mode switch
    {
        AppThemeMode.Dark => true,
        AppThemeMode.Light => false,
        _ => Application.Current?.RequestedTheme == AppTheme.Dark
    };

    public event Action? Changed;

    public Task InitializeAsync()
    {
        try
        {
            var stored = Preferences.Default.Get(ThemeKey, (int)AppThemeMode.System);
            Mode = Enum.IsDefined(typeof(AppThemeMode), stored) ? (AppThemeMode)stored : AppThemeMode.System;

            System.Diagnostics.Debug.WriteLine($"Theme initialized to mode: {Mode}");

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

    public Task SetModeAsync(AppThemeMode mode)
    {
        try
        {
            Mode = mode;
            Preferences.Default.Set(ThemeKey, (int)mode);

            System.Diagnostics.Debug.WriteLine($"Theme changed to mode: {Mode}");

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

    private void ApplyModeToMaui()
    {
        try
        {
            if (Application.Current is null)
            {
                System.Diagnostics.Debug.WriteLine("Application.Current is null, cannot apply theme");
                return;
            }

            Application.Current.UserAppTheme = Mode switch
            {
                AppThemeMode.Light => AppTheme.Light,
                AppThemeMode.Dark => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };

            System.Diagnostics.Debug.WriteLine($"Applied theme to MAUI: {Application.Current.UserAppTheme}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error applying theme to MAUI: {ex.Message}");
        }
    }
}

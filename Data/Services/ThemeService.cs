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
        var stored = Preferences.Default.Get(ThemeKey, (int)AppThemeMode.System);
        Mode = Enum.IsDefined(typeof(AppThemeMode), stored) ? (AppThemeMode)stored : AppThemeMode.System;
        ApplyModeToMaui();
        Changed?.Invoke();
        return Task.CompletedTask;
    }

 public Task SetModeAsync(AppThemeMode mode)
 {
        Mode = mode;
        Preferences.Default.Set(ThemeKey, (int)mode);
        ApplyModeToMaui();
        Changed?.Invoke();
        return Task.CompletedTask;
    }

 private void ApplyModeToMaui()
 {
        if (Application.Current is null) return;

        Application.Current.UserAppTheme = Mode switch
        {
            AppThemeMode.Light => AppTheme.Light,
            AppThemeMode.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }
}

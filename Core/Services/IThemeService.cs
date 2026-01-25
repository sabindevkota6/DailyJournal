using System;
using System.Threading.Tasks;

namespace DailyJournal.Core.Services;

public enum AppThemeMode
{
 System =0,
 Light =1,
 Dark =2
}

public interface IThemeService
{
 AppThemeMode Mode { get; }
 bool IsDarkMode { get; }
 event Action? Changed;

 Task InitializeAsync();
 Task SetModeAsync(AppThemeMode mode);
}

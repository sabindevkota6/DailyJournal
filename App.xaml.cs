using DailyJournal.Core.Services;

namespace DailyJournal
{
    public partial class App : Application
    {
        public App(IDatabaseService databaseService, IThemeService themeService)
        {
            InitializeComponent();

            // Add global exception handlers
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            try
            {
                databaseService.InitializeAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization failed: {ex}");
            }

            try
            {
                themeService.InitializeAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Theme initialization failed: {ex}");
            }

            MainPage = new MainPage();
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== UNHANDLED EXCEPTION ===");
                System.Diagnostics.Debug.WriteLine($"Exception Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }

                System.Diagnostics.Debug.WriteLine("=========================");
            }
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== UNOBSERVED TASK EXCEPTION ===");
            System.Diagnostics.Debug.WriteLine($"Exception: {e.Exception}");
            System.Diagnostics.Debug.WriteLine("================================");
            e.SetObserved(); // Prevent the app from crashing
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(MainPage) { Title = "SabinDevkota" };
        }
    }
}

using DailyJournal.Core.Services;

namespace DailyJournal
{
    // main application class that handles startup and global exception handling
    public partial class App : Application
    {
        // constructor that initializes database, theme, and sets up exception handlers
        public App(IDatabaseService databaseService, IThemeService themeService)
        {
            InitializeComponent();

            // adding global exception handlers to catch unhandled errors
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // initializing the database synchronously at startup
            try
            {
                databaseService.InitializeAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization failed: {ex}");
            }

            // initializing the theme service synchronously at startup
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

        // handling any unhandled exceptions in the app domain
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

        // handling exceptions from unobserved tasks (async operations that werent awaited)
        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== UNOBSERVED TASK EXCEPTION ===");
            System.Diagnostics.Debug.WriteLine($"Exception: {e.Exception}");
            System.Diagnostics.Debug.WriteLine("================================");
            e.SetObserved(); // preventing the app from crashing
        }

        // creating the main window with a custom title
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(MainPage) { Title = "SabinDevkota" };
        }
    }
}

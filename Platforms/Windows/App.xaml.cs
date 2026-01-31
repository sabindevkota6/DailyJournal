namespace DailyJournal.WinUI
{
    // windows specific application class for WinUI
    public partial class App : MauiWinUIApplication
    {
        // initializing the singleton application object
        public App()
        {
            this.InitializeComponent();
        }

        // creating and returning the MAUI application
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}

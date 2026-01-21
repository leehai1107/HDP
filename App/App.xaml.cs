using App.Services;

namespace App
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();
            
            // Navigate to login page by default
            shell.Navigated += async (s, e) =>
            {
                if (e.Current?.Location.OriginalString == "//Login")
                    return;
                
                // Check if user is authenticated
                var authService = Handler?.MauiContext?.Services?.GetService<IAuthenticationService>();
                if (authService != null && !authService.IsAuthenticated)
                {
                    await shell.GoToAsync("//Login");
                }
            };
            
            return new Window(shell);
        }
    }
}
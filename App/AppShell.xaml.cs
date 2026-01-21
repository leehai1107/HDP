using App.Services;

namespace App
{
    public partial class AppShell : Shell
    {
        private readonly IAuthenticationService? _authService;

        public AppShell()
        {
            InitializeComponent();
            
            // Try to get auth service
            _authService = Handler?.MauiContext?.Services?.GetService<IAuthenticationService>();
            
            Navigated += OnNavigated;
        }

        private void OnNavigated(object? sender, ShellNavigatedEventArgs e)
        {
            // Hide/show tabs based on admin status
            UpdateTabVisibility();
        }

        private void UpdateTabVisibility()
        {
            if (_authService == null)
                return;

            // Find the File Explorer tab and show only to admins
            var fileExplorerTab = Items.FirstOrDefault(item => item.Route == "FileExplorer");
            if (fileExplorerTab != null)
            {
                fileExplorerTab.IsVisible = _authService.IsAdmin;
            }

            // Find the Users tab and show only to admins
            var usersTab = Items.FirstOrDefault(item => item.Route == "Users");
            if (usersTab != null)
            {
                usersTab.IsVisible = _authService.IsAdmin;
            }
        }
    }
}

using System.ComponentModel;
using System.Runtime.CompilerServices;
using App.Services;

namespace App
{
    public partial class LoginPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IAuthenticationService _authService;
        
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _hasError = false;

        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasError
        {
            get => _hasError;
            set
            {
                if (_hasError != value)
                {
                    _hasError = value;
                    OnPropertyChanged();
                }
            }
        }

        public LoginPage(IAuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
            BindingContext = this;
        }

        private async void OnLoginClicked(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter username and password";
                HasError = true;
                return;
            }

            if (_authService.Login(Username, Password))
            {
                // Login successful, navigate based on role
                if (_authService.IsAdmin)
                {
                    await Shell.Current.GoToAsync("//FileExplorer");
                }
                else
                {
                    await Shell.Current.GoToAsync("//Tasks");
                }
            }
            else
            {
                ErrorMessage = "Invalid username or password";
                HasError = true;
                Password = string.Empty;
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

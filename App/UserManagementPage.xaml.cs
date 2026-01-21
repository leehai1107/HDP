using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using App.Models;
using App.Services;

namespace App
{
    public partial class UserManagementPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IAuthenticationService _authService;

        private ObservableCollection<User> _users = new();
        private bool _isFormVisible = false;
        private string _formTitle = "Add User";
        private string _editUsername = string.Empty;
        private string _editPassword = string.Empty;
        private string _editDisplayName = string.Empty;
        private string _editRole = "User";
        private User? _editingUser = null;

        public ObservableCollection<User> Users
        {
            get => _users;
            set
            {
                if (_users != value)
                {
                    _users = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> Roles { get; } = new() { "Admin", "User" };

        public bool IsFormVisible
        {
            get => _isFormVisible;
            set
            {
                if (_isFormVisible != value)
                {
                    _isFormVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FormTitle
        {
            get => _formTitle;
            set
            {
                if (_formTitle != value)
                {
                    _formTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        public string EditUsername
        {
            get => _editUsername;
            set
            {
                if (_editUsername != value)
                {
                    _editUsername = value;
                    OnPropertyChanged();
                }
            }
        }

        public string EditPassword
        {
            get => _editPassword;
            set
            {
                if (_editPassword != value)
                {
                    _editPassword = value;
                    OnPropertyChanged();
                }
            }
        }

        public string EditDisplayName
        {
            get => _editDisplayName;
            set
            {
                if (_editDisplayName != value)
                {
                    _editDisplayName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string EditRole
        {
            get => _editRole;
            set
            {
                if (_editRole != value)
                {
                    _editRole = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand SaveUserCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ToggleUserActiveCommand { get; }
        public ICommand LogoutCommand { get; }

        public UserManagementPage(IAuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
            BindingContext = this;

            AddUserCommand = new Command(OnAddUser);
            EditUserCommand = new Command<User>(OnEditUser);
            SaveUserCommand = new Command(OnSaveUser);
            CancelEditCommand = new Command(OnCancelEdit);
            DeleteUserCommand = new Command<User>(OnDeleteUser);
            ToggleUserActiveCommand = new Command<User>(OnToggleUserActive);
            LogoutCommand = new Command(OnLogout);

            LoadUsers();
        }

        private void LoadUsers()
        {
            Users.Clear();
            foreach (var user in _authService.GetAllUsers())
            {
                Users.Add(user);
            }
        }

        private void OnAddUser()
        {
            _editingUser = null;
            FormTitle = "Add User";
            EditUsername = string.Empty;
            EditPassword = string.Empty;
            EditDisplayName = string.Empty;
            EditRole = "User";
            IsFormVisible = true;
        }

        private void OnEditUser(User user)
        {
            if (user == null) return;

            _editingUser = user;
            FormTitle = "Edit User";
            EditUsername = user.Username;
            EditPassword = user.Password;
            EditDisplayName = user.DisplayName;
            EditRole = user.Role;
            IsFormVisible = true;
        }

        private async void OnSaveUser()
        {
            if (string.IsNullOrWhiteSpace(EditUsername) || string.IsNullOrWhiteSpace(EditPassword))
            {
                await DisplayAlert("Error", "Username and password are required", "OK");
                return;
            }

            bool success;
            if (_editingUser == null)
            {
                // Add new user
                var newUser = new User
                {
                    Username = EditUsername,
                    Password = EditPassword,
                    DisplayName = string.IsNullOrWhiteSpace(EditDisplayName) ? EditUsername : EditDisplayName,
                    Role = EditRole,
                    IsActive = true
                };
                success = _authService.AddUser(newUser);
            }
            else
            {
                // Update existing user
                _editingUser.Username = EditUsername;
                _editingUser.Password = EditPassword;
                _editingUser.DisplayName = string.IsNullOrWhiteSpace(EditDisplayName) ? EditUsername : EditDisplayName;
                _editingUser.Role = EditRole;
                success = _authService.UpdateUser(_editingUser);
            }

            if (success)
            {
                LoadUsers();
                IsFormVisible = false;
            }
            else
            {
                await DisplayAlert("Error", "Failed to save user. Username might already exist.", "OK");
            }
        }

        private void OnCancelEdit()
        {
            IsFormVisible = false;
            _editingUser = null;
        }

        private async void OnDeleteUser(User user)
        {
            if (user == null) return;

            bool confirm = await DisplayAlert("Confirm Delete", 
                $"Are you sure you want to delete user '{user.Username}'?", "Delete", "Cancel");
            
            if (confirm)
            {
                if (_authService.DeleteUser(user.Id))
                {
                    LoadUsers();
                }
                else
                {
                    await DisplayAlert("Error", "Failed to delete user. Cannot delete admin users.", "OK");
                }
            }
        }

        private void OnToggleUserActive(User user)
        {
            if (user == null) return;

            user.IsActive = !user.IsActive;
            _authService.UpdateUser(user);
            LoadUsers();
        }

        private async void OnLogout()
        {
            bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
            if (confirm)
            {
                _authService.Logout();
                await Shell.Current.GoToAsync("//Login");
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

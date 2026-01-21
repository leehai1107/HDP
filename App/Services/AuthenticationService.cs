using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Configuration;
using App.Models;

namespace App.Services
{
    public interface IAuthenticationService
    {
        User? CurrentUser { get; }
        bool IsAuthenticated { get; }
        bool IsAdmin { get; }
        bool Login(string username, string password);
        void Logout();
        List<User> GetAllUsers();
        bool AddUser(User user);
        bool UpdateUser(User user);
        bool DeleteUser(string userId);
        List<User> GetAssignableUsers();
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IConfiguration _configuration;
        private readonly string _usersFilePath;
        private List<User> _users;

        public User? CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;
        public bool IsAdmin => CurrentUser?.Role == "Admin";

        public AuthenticationService(IConfiguration configuration)
        {
            _configuration = configuration;
            _usersFilePath = Path.Combine(FileSystem.AppDataDirectory, "users.json");
            _users = LoadUsers();
        }

        private List<User> LoadUsers()
        {
            try
            {
                if (File.Exists(_usersFilePath))
                {
                    var json = File.ReadAllText(_usersFilePath);
                    return JsonSerializer.Deserialize<List<User>>(json) ?? GetDefaultUsers();
                }
            }
            catch { }

            // Return default users from config
            return GetDefaultUsers();
        }

        private List<User> GetDefaultUsers()
        {
            var defaultUsers = new List<User>();
            
            // Get default admin from appsettings
            var adminUsername = _configuration["Authentication:DefaultAdmin:Username"] ?? "admin";
            var adminPassword = _configuration["Authentication:DefaultAdmin:Password"] ?? "admin";
            
            defaultUsers.Add(new User
            {
                Username = adminUsername,
                Password = adminPassword,
                Role = "Admin",
                DisplayName = "Administrator",
                CreatedDate = DateTime.Now,
                IsActive = true
            });

            SaveUsers(defaultUsers);
            return defaultUsers;
        }

        private void SaveUsers(List<User> users)
        {
            try
            {
                var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_usersFilePath, json);
            }
            catch { }
        }

        public bool Login(string username, string password)
        {
            var user = _users.FirstOrDefault(u => 
                u.Username == username && 
                u.Password == password && 
                u.IsActive);

            if (user != null)
            {
                CurrentUser = user;
                return true;
            }

            return false;
        }

        public void Logout()
        {
            CurrentUser = null;
        }

        public List<User> GetAllUsers()
        {
            if (!IsAdmin)
                return new List<User>();

            return _users.ToList();
        }

        public bool AddUser(User user)
        {
            if (!IsAdmin)
                return false;

            if (_users.Any(u => u.Username == user.Username))
                return false;

            _users.Add(user);
            SaveUsers(_users);
            return true;
        }

        public bool UpdateUser(User user)
        {
            if (!IsAdmin)
                return false;

            var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existingUser == null)
                return false;

            existingUser.Username = user.Username;
            existingUser.Password = user.Password;
            existingUser.Role = user.Role;
            existingUser.DisplayName = user.DisplayName;
            existingUser.IsActive = user.IsActive;

            SaveUsers(_users);
            return true;
        }

        public bool DeleteUser(string userId)
        {
            if (!IsAdmin)
                return false;

            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null || user.Role == "Admin")
                return false; // Don't delete admin

            _users.Remove(user);
            SaveUsers(_users);
            return true;
        }

        public List<User> GetAssignableUsers()
        {
            return _users.Where(u => u.IsActive).ToList();
        }
    }
}

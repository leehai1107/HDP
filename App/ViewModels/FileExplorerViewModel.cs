using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using App.Models;
using App.Services;

namespace App.ViewModels
{

    public class FileExplorerViewModel : INotifyPropertyChanged
    {
        private readonly IFileExplorerService _fileService;
        private readonly IConfigurationService _configService;
        private readonly ITaskService _taskService;
        private readonly Stack<string> _backStack = new();
        private readonly Stack<string> _forwardStack = new();

        private string _currentPath = string.Empty;
        private bool _isLoading = false;
        private string _newFolderName = string.Empty;
        private bool _canGoBack = false;
        private bool _canGoForward = false;
        private bool _isSelectionMode = false;

        // Task properties
        private string _newTaskName = string.Empty;
        private string _newTaskDescription = string.Empty;
        private DateTime _newTaskEndDate = DateTime.Now.AddDays(1);
        private string _newTaskAttachmentPath = string.Empty;

        public ObservableCollection<FileItem> Items { get; } = new();
        public ObservableCollection<FileItem> SelectedItems { get; } = new();
        public ObservableCollection<TaskItem> Tasks { get; } = new();

        private void OnSelectedItemsChanged()
        {
            ((Command)DeleteSelectedCommand).ChangeCanExecute();
        }
        
        public string CurrentPath
        {
            get => _currentPath;
            set
            {
                if (_currentPath != value)
                {
                    _currentPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public string NewFolderName
        {
            get => _newFolderName;
            set
            {
                if (_newFolderName != value)
                {
                    _newFolderName = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CanGoBack
        {
            get => _canGoBack;
            private set
            {
                if (_canGoBack != value)
                {
                    _canGoBack = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CanGoForward
        {
            get => _canGoForward;
            private set
            {
                if (_canGoForward != value)
                {
                    _canGoForward = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelectionMode
        {
            get => _isSelectionMode;
            set
            {
                if (_isSelectionMode != value)
                {
                    _isSelectionMode = value;
                    OnPropertyChanged();
                    
                    // Clear selections when exiting selection mode
                    if (!value)
                    {
                        ClearSelections();
                    }
                }
            }
        }

        // Task Properties
        public string NewTaskName
        {
            get => _newTaskName;
            set
            {
                if (_newTaskName != value)
                {
                    _newTaskName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string NewTaskDescription
        {
            get => _newTaskDescription;
            set
            {
                if (_newTaskDescription != value)
                {
                    _newTaskDescription = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime NewTaskEndDate
        {
            get => _newTaskEndDate;
            set
            {
                if (_newTaskEndDate != value)
                {
                    _newTaskEndDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public string NewTaskAttachmentPath
        {
            get => _newTaskAttachmentPath;
            set
            {
                if (_newTaskAttachmentPath != value)
                {
                    _newTaskAttachmentPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand NavigateCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand GoForwardCommand { get; }
        public ICommand GoPreviousCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CreateFolderCommand { get; }
        public ICommand ToggleSelectionModeCommand { get; }
        public ICommand ToggleItemSelectionCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        
        // Task Commands
        public ICommand AddTaskCommand { get; }
        public ICommand ToggleTaskDoneCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand NavigateToTaskPathCommand { get; }
        public ICommand SetCurrentPathAsAttachmentCommand { get; }

        public FileExplorerViewModel(IFileExplorerService fileService, IConfigurationService configService, ITaskService taskService)
        {
            _fileService = fileService;
            _configService = configService;
            _taskService = taskService;

            // Subscribe to collection changes
            SelectedItems.CollectionChanged += (s, e) => OnSelectedItemsChanged();

            NavigateCommand = new Command<FileItem>(async (item) => await NavigateToAsync(item));
            GoBackCommand = new Command(async () => await GoBackAsync());
            GoPreviousCommand = new Command(async () => await GoPreviousAsync(), () => CanGoBack);
            GoForwardCommand = new Command(async () => await GoForwardAsync(), () => CanGoForward);
            RefreshCommand = new Command(async () => await LoadItemsAsync());
            DeleteCommand = new Command<FileItem>(async (item) => await DeleteAsync(item));
            CreateFolderCommand = new Command(async () => await CreateFolderAsync());
            ToggleSelectionModeCommand = new Command(() => IsSelectionMode = !IsSelectionMode);
            ToggleItemSelectionCommand = new Command<FileItem>(ToggleItemSelection);
            RenameCommand = new Command<FileItem>(async (item) => await RenameItemAsync(item));
            DeleteSelectedCommand = new Command(async () => await DeleteSelectedAsync(), () => SelectedItems.Count > 0);
            
            // Task Commands
            AddTaskCommand = new Command(async () => await AddTaskAsync());
            ToggleTaskDoneCommand = new Command<TaskItem>(async (task) => await ToggleTaskDoneAsync(task));
            DeleteTaskCommand = new Command<TaskItem>(async (task) => await DeleteTaskAsync(task));
            NavigateToTaskPathCommand = new Command<TaskItem>(async (task) => await NavigateToTaskPathAsync(task));
            SetCurrentPathAsAttachmentCommand = new Command(() => NewTaskAttachmentPath = CurrentPath);
        }

        public async Task InitializeAsync()
        {
            CurrentPath = _configService.GetRootPath();
            await LoadItemsAsync();
            await LoadTasksAsync();
        }

        private async Task LoadTasksAsync()
        {
            var tasks = await _taskService.GetAllTasksAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Tasks.Clear();
                foreach (var task in tasks.OrderBy(t => t.CreateDate))
                {
                    Tasks.Add(task);
                }
            });
        }

        private async Task LoadItemsAsync()
        {
            if (!_fileService.PathExists(CurrentPath))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Items.Clear();
                });
                return;
            }

            IsLoading = true;
            try
            {
                var items = await _fileService.GetFilesAndFoldersAsync(CurrentPath);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Items.Clear();
                    foreach (var item in items)
                    {
                        Items.Add(item);
                    }
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task NavigateToAsync(FileItem item)
        {
            if (item == null) return;
            
            if (item.IsDirectory)
            {
                await NavigateToPath(item.FullPath, true);
            }
            else
            {
                // Open file with default application
                try
                {
                    await Launcher.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(item.FullPath)
                    });
                }
                catch
                {
                    await App.Current?.MainPage?.DisplayAlert("Error", "Cannot open this file", "OK");
                }
            }
        }

        private async Task NavigateToPath(string path, bool addToHistory = true)
        {
            if (addToHistory && !string.IsNullOrEmpty(CurrentPath))
            {
                _backStack.Push(CurrentPath);
                _forwardStack.Clear();
                UpdateNavigationState();
            }

            CurrentPath = path;
            await LoadItemsAsync();
        }

        private async Task GoPreviousAsync()
        {
            if (_backStack.Count > 0)
            {
                _forwardStack.Push(CurrentPath);
                var previousPath = _backStack.Pop();
                CurrentPath = previousPath;
                await LoadItemsAsync();
                UpdateNavigationState();
            }
        }

        private async Task GoForwardAsync()
        {
            if (_forwardStack.Count > 0)
            {
                _backStack.Push(CurrentPath);
                var nextPath = _forwardStack.Pop();
                CurrentPath = nextPath;
                await LoadItemsAsync();
                UpdateNavigationState();
            }
        }

        private async Task GoBackAsync()
        {
            var parentPath = _fileService.GetParentDirectory(CurrentPath);
            if (parentPath != CurrentPath)
            {
                await NavigateToPath(parentPath, true);
            }
        }

        private void UpdateNavigationState()
        {
            CanGoBack = _backStack.Count > 0;
            CanGoForward = _forwardStack.Count > 0;
            ((Command)GoPreviousCommand).ChangeCanExecute();
            ((Command)GoForwardCommand).ChangeCanExecute();
        }

        private async Task DeleteAsync(FileItem item)
        {
            var result = await App.Current?.MainPage?.DisplayAlert("Delete", 
                $"Are you sure you want to delete '{item.Name}'?", "Yes", "No");
            
            if (result == true)
            {
                if (await _fileService.DeleteAsync(item.FullPath))
                {
                    await LoadItemsAsync();
                }
                else
                {
                    await App.Current?.MainPage?.DisplayAlert("Error", "Failed to delete item", "OK");
                }
            }
        }

        private async Task CreateFolderAsync()
        {
            if (string.IsNullOrWhiteSpace(NewFolderName))
            {
                await App.Current?.MainPage?.DisplayAlert("Error", "Please enter a folder name", "OK");
                return;
            }

            if (await _fileService.CreateFolderAsync(CurrentPath, NewFolderName))
            {
                NewFolderName = string.Empty;
                await LoadItemsAsync();
            }
            else
            {
                await App.Current?.MainPage?.DisplayAlert("Error", "Failed to create folder", "OK");
            }
        }

        private void ToggleItemSelection(FileItem item)
        {
            if (item == null) return;

            item.IsSelected = !item.IsSelected;

            if (item.IsSelected && !SelectedItems.Contains(item))
            {
                SelectedItems.Add(item);
            }
            else if (!item.IsSelected && SelectedItems.Contains(item))
            {
                SelectedItems.Remove(item);
            }

            ((Command)DeleteSelectedCommand).ChangeCanExecute();
        }

        private void ClearSelections()
        {
            foreach (var item in Items)
            {
                item.IsSelected = false;
            }
            SelectedItems.Clear();
            ((Command)DeleteSelectedCommand).ChangeCanExecute();
        }

        private async Task RenameItemAsync(FileItem item)
        {
            if (item == null) return;

            var result = await App.Current?.MainPage?.DisplayPromptAsync(
                "Rename", 
                "Enter new name:", 
                "OK", 
                "Cancel", 
                item.Name);

            if (!string.IsNullOrWhiteSpace(result) && result != item.Name)
            {
                if (await _fileService.RenameAsync(item.FullPath, result))
                {
                    await LoadItemsAsync();
                }
                else
                {
                    await App.Current?.MainPage?.DisplayAlert("Error", 
                        "Failed to rename item. Name may already exist.", "OK");
                }
            }
        }

        private async Task DeleteSelectedAsync()
        {
            if (SelectedItems.Count == 0) return;

            var result = await App.Current?.MainPage?.DisplayAlert("Delete", 
                $"Are you sure you want to delete {SelectedItems.Count} item(s)?", "Yes", "No");
            
            if (result == true)
            {
                var itemsToDelete = SelectedItems.ToList();
                var failures = 0;

                foreach (var item in itemsToDelete)
                {
                    if (!await _fileService.DeleteAsync(item.FullPath))
                    {
                        failures++;
                    }
                }

                ClearSelections();
                await LoadItemsAsync();

                if (failures > 0)
                {
                    await App.Current?.MainPage?.DisplayAlert("Warning", 
                        $"Failed to delete {failures} item(s)", "OK");
                }
            }
        }

        // Task Management Methods
        private async Task AddTaskAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTaskName))
            {
                await App.Current?.MainPage?.DisplayAlert("Error", "Please enter a task name", "OK");
                return;
            }

            var task = new TaskItem
            {
                Name = NewTaskName,
                Description = NewTaskDescription,
                CreateDate = DateTime.Now,
                EndDate = NewTaskEndDate,
                AttachmentPath = NewTaskAttachmentPath,
                IsDone = false
            };

            if (await _taskService.AddTaskAsync(task))
            {
                Tasks.Add(task);
                
                // Clear form
                NewTaskName = string.Empty;
                NewTaskDescription = string.Empty;
                NewTaskEndDate = DateTime.Now.AddDays(1);
                NewTaskAttachmentPath = string.Empty;
            }
            else
            {
                await App.Current?.MainPage?.DisplayAlert("Error", "Failed to create task", "OK");
            }
        }

        public async Task ToggleTaskDoneAsync(TaskItem task)
        {
            if (task == null) return;

            // Don't toggle here - the CheckBox binding already updated IsDone
            // Just save the updated state to persistence
            await _taskService.UpdateTaskAsync(task);
        }

        private async Task DeleteTaskAsync(TaskItem task)
        {
            if (task == null) return;

            var result = await App.Current?.MainPage?.DisplayAlert("Delete Task", 
                $"Are you sure you want to delete '{task.Name}'?", "Yes", "No");
            
            if (result == true)
            {
                if (await _taskService.DeleteTaskAsync(task.Id))
                {
                    Tasks.Remove(task);
                }
                else
                {
                    await App.Current?.MainPage?.DisplayAlert("Error", "Failed to delete task", "OK");
                }
            }
        }

        private async Task NavigateToTaskPathAsync(TaskItem task)
        {
            if (task == null || string.IsNullOrWhiteSpace(task.AttachmentPath))
                return;

            if (_fileService.PathExists(task.AttachmentPath))
            {
                await NavigateToPath(task.AttachmentPath, true);
            }
            else
            {
                await App.Current?.MainPage?.DisplayAlert("Error", 
                    "The attached folder path does not exist or is not accessible.", "OK");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

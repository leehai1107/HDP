using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using App.Models;
using App.Services;
using App.Helpers;

namespace App.ViewModels
{

    public class FileExplorerViewModel : INotifyPropertyChanged
    {
        private readonly IFileExplorerService _fileService;
        private readonly IConfigurationService _configService;
        private readonly IFileIndexService _indexService;
        private readonly IAuthenticationService _authService;
        private readonly Stack<string> _backStack = new();
        private readonly Stack<string> _forwardStack = new();
        private System.Threading.Timer? _searchDebounceTimer;
        private System.Threading.CancellationTokenSource? _searchCancellationTokenSource;

        private string _currentPath = string.Empty;
        private bool _isLoading = false;
        private bool _isSearching = false;
        private bool _isIndexing = false;
        private string _searchStatus = string.Empty;
        private string _indexStatus = string.Empty;
        private string _newFolderName = string.Empty;
        private bool _canGoBack = false;
        private bool _canGoForward = false;
        private bool _isSelectionMode = false;
        private string _fileSearchQuery = string.Empty;

        public ObservableCollection<FileItem> Items { get; } = new();
        public ObservableCollection<FileItem> SelectedItems { get; } = new();
        public ObservableCollection<FileItem> FilteredItems { get; } = new();

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

        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                if (_isSearching != value)
                {
                    _isSearching = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SearchStatus
        {
            get => _searchStatus;
            set
            {
                if (_searchStatus != value)
                {
                    _searchStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsIndexing
        {
            get => _isIndexing;
            set
            {
                if (_isIndexing != value)
                {
                    _isIndexing = value;
                    OnPropertyChanged();
                }
            }
        }

        public string IndexStatus
        {
            get => _indexStatus;
            set
            {
                if (_indexStatus != value)
                {
                    _indexStatus = value;
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



        public string FileSearchQuery
        {
            get => _fileSearchQuery;
            set
            {
                if (_fileSearchQuery != value)
                {
                    _fileSearchQuery = value;
                    OnPropertyChanged();
                    DebouncedSearch();
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
        public ICommand CopyPathCommand { get; }
        public ICommand BuildIndexCommand { get; }

        public string CurrentUsername => _authService.CurrentUser?.Username ?? "Guest";

        public FileExplorerViewModel(IFileExplorerService fileService, IConfigurationService configService, IFileIndexService indexService, IAuthenticationService authService)
        {
            _fileService = fileService;
            _configService = configService;
            _indexService = indexService;
            _authService = authService;

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
            CopyPathCommand = new Command(async () => await CopyPathAsync());
            BuildIndexCommand = new Command(async () => await BuildIndexAsync());
        }

        public async Task InitializeAsync()
        {
            CurrentPath = _configService.GetRootPath();
            await LoadItemsAsync();
            
            // Build index automatically in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        IndexStatus = "Building index in background...";
                    });

                    var progress = new Progress<string>(status =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            IndexStatus = status;
                        });
                    });

                    await _indexService.BuildIndexAsync(CurrentPath, progress);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        IndexStatus = $"⚡ Index ready: {_indexService.IndexedFileCount:N0} files - Search is instant!";
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Auto-index failed: {ex.Message}");
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        IndexStatus = string.Empty;
                    });
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
                
                // Natural sort (folders first, then files, with numeric sorting)
                var sortedItems = items
                    .OrderBy(item => item.IsDirectory ? 0 : 1) // Folders first
                    .ThenBy(item => item.Name, new NaturalStringComparer())
                    .ToList();
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Items.Clear();
                    foreach (var item in sortedItems)
                    {
                        Items.Add(item);
                    }
                    FilterFiles(); // Update filtered collection
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

        private void DebouncedSearch()
        {
            // Cancel any ongoing search
            _searchCancellationTokenSource?.Cancel();
            
            // Dispose and reset the debounce timer
            _searchDebounceTimer?.Dispose();
            
            if (string.IsNullOrWhiteSpace(FileSearchQuery))
            {
                // Immediately show all items if search is cleared
                FilteredItems.Clear();
                foreach (var item in Items)
                {
                    FilteredItems.Add(item);
                }
                SearchStatus = string.Empty;
                IsSearching = false;
            }
            else
            {
                // Wait 300ms before starting search
                SearchStatus = "Typing...";
                _searchDebounceTimer = new System.Threading.Timer(
                    async _ => await PerformSearchAsync(),
                    null,
                    300,
                    System.Threading.Timeout.Infinite);
            }
        }

        private async Task PerformSearchAsync()
        {
            var query = FileSearchQuery;
            if (string.IsNullOrWhiteSpace(query))
                return;

            // Create new cancellation token
            _searchCancellationTokenSource = new System.Threading.CancellationTokenSource();
            var cancellationToken = _searchCancellationTokenSource.Token;

            try
            {
                IsSearching = true;
                SearchStatus = $"Searching for '{query}'...";

                // Clear previous results
                await MainThread.InvokeOnMainThreadAsync(() => FilteredItems.Clear());

                List<FileItem> searchResults;
                bool usedIndex = false;
                
                // Use index if available and current path is indexed or is within indexed path
                if (_indexService.IsIndexed && 
                    (CurrentPath.StartsWith(_indexService.IndexedPath, StringComparison.OrdinalIgnoreCase) ||
                     _indexService.IndexedPath.StartsWith(CurrentPath, StringComparison.OrdinalIgnoreCase)))
                {
                    // Search the index
                    searchResults = await _indexService.SearchIndexAsync(query, cancellationToken);
                    
                    // Filter to only current path if we're in a subfolder
                    if (!CurrentPath.Equals(_indexService.IndexedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        searchResults = searchResults
                            .Where(item => item.FullPath.StartsWith(CurrentPath, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                    usedIndex = true;
                }
                else
                {
                    // Fall back to direct file system search
                    searchResults = await _fileService.SearchFilesAsync(CurrentPath, query, cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                // Update UI on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var item in searchResults.OrderByDescending(x => x.IsDirectory).ThenBy(x => x.RelativePath))
                    {
                        FilteredItems.Add(item);
                    }

                    var indexedNote = usedIndex ? " ⚡" : "";
                    SearchStatus = searchResults.Count == 0 
                        ? "No results found" 
                        : $"Found {searchResults.Count} result{(searchResults.Count == 1 ? "" : "s")}{indexedNote}";
                });
            }
            catch (OperationCanceledException)
            {
                // Search was cancelled, ignore
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    SearchStatus = "Search error";
                    await App.Current?.MainPage?.DisplayAlert("Search Error",
                        $"Error searching files: {ex.Message}", "OK");
                });
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() => IsSearching = false);
            }
        }

        private void FilterFiles()
        {
            FilteredItems.Clear();
            foreach (var item in Items)
            {
                FilteredItems.Add(item);
            }
        }

        private async Task CopyPathAsync()
        {
            if (!string.IsNullOrWhiteSpace(CurrentPath))
            {
                await Clipboard.SetTextAsync(CurrentPath);
                await App.Current?.MainPage?.DisplayAlert("Copied", "Path copied to clipboard", "OK");
            }
        }

        private async Task BuildIndexAsync()
        {
            try
            {
                IsIndexing = true;
                IndexStatus = "Starting index build...";

                var progress = new Progress<string>(status =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        IndexStatus = status;
                    });
                });

                await _indexService.RebuildIndexAsync(CurrentPath, progress);

                IndexStatus = $"Index ready: {_indexService.IndexedFileCount:N0} files. Search is now instant!";

                // Show notification
                await App.Current?.MainPage?.DisplayAlert("Index Built", 
                    $"Indexed {_indexService.IndexedFileCount:N0} files.\\nSearch is now instant!", "OK");
            }
            catch (Exception ex)
            {
                IndexStatus = "Index build failed";
                await App.Current?.MainPage?.DisplayAlert("Error", 
                    $"Failed to build index: {ex.Message}", "OK");
            }
            finally
            {
                IsIndexing = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using App.Models;

namespace App.Services
{
    public interface IFileExplorerService
    {
        Task<List<FileItem>> GetFilesAndFoldersAsync(string path);
        Task<List<FileItem>> SearchFilesAsync(string rootPath, string searchQuery, System.Threading.CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string path);
        Task<bool> CreateFolderAsync(string parentPath, string folderName);
        Task<bool> RenameAsync(string oldPath, string newName);
        bool PathExists(string path);
        string GetParentDirectory(string path);
    }

    public class FileExplorerService : IFileExplorerService
    {
        public async Task<List<FileItem>> GetFilesAndFoldersAsync(string path)
        {
            var items = new List<FileItem>();

            try
            {
                if (!Directory.Exists(path))
                    return items;

                // Get directories
                var directories = new DirectoryInfo(path).GetDirectories();
                foreach (var dir in directories)
                {
                    try
                    {
                        items.Add(new FileItem
                        {
                            Name = dir.Name,
                            FullPath = dir.FullName,
                            IsDirectory = true,
                            Modified = dir.LastWriteTime
                        });
                    }
                    catch
                    {
                        // Skip if we don't have permission
                    }
                }

                // Get files
                var files = new DirectoryInfo(path).GetFiles();
                foreach (var file in files)
                {
                    try
                    {
                        items.Add(new FileItem
                        {
                            Name = file.Name,
                            FullPath = file.FullName,
                            IsDirectory = false,
                            Size = file.Length,
                            Modified = file.LastWriteTime
                        });
                    }
                    catch
                    {
                        // Skip if we don't have permission
                    }
                }

                // Sort: directories first, then alphabetically
                items = items.OrderByDescending(x => x.IsDirectory)
                             .ThenBy(x => x.Name)
                             .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting files and folders: {ex.Message}");
            }

            return await Task.FromResult(items);
        }

        public async Task<bool> DeleteAsync(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return await Task.FromResult(true);
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    return await Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting: {ex.Message}");
            }

            return await Task.FromResult(false);
        }

        public async Task<bool> CreateFolderAsync(string parentPath, string folderName)
        {
            try
            {
                var newPath = Path.Combine(parentPath, folderName);
                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                    return await Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating folder: {ex.Message}");
            }

            return await Task.FromResult(false);
        }

        public async Task<bool> RenameAsync(string oldPath, string newName)
        {
            try
            {
                var directory = Path.GetDirectoryName(oldPath);
                if (string.IsNullOrEmpty(directory))
                    return false;

                var newPath = Path.Combine(directory, newName);
                
                // Check if target already exists
                if (File.Exists(newPath) || Directory.Exists(newPath))
                    return false;

                // Rename file or directory
                if (File.Exists(oldPath))
                {
                    File.Move(oldPath, newPath);
                }
                else if (Directory.Exists(oldPath))
                {
                    Directory.Move(oldPath, newPath);
                }
                else
                {
                    return false;
                }

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error renaming: {ex.Message}");
            }

            return await Task.FromResult(false);
        }

        public bool PathExists(string path)
        {
            return Directory.Exists(path) || File.Exists(path);
        }

        public string GetParentDirectory(string path)
        {
            var parentInfo = new DirectoryInfo(path).Parent;
            return parentInfo?.FullName ?? path;
        }

        public async Task<List<FileItem>> SearchFilesAsync(string rootPath, string searchQuery, System.Threading.CancellationToken cancellationToken = default)
        {
            const int maxResults = 1000;

            if (string.IsNullOrWhiteSpace(searchQuery) || !Directory.Exists(rootPath))
                return new List<FileItem>();

            var results = new System.Collections.Concurrent.ConcurrentBag<FileItem>();

            await Task.Run(() =>
            {
                try
                {
                    // Use breadth-first search for faster initial results
                    SearchBreadthFirst(rootPath, searchQuery, results, maxResults, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Search cancelled by user
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error searching files: {ex.Message}");
                }
            }, cancellationToken);

            return results.ToList();
        }

        private static readonly HashSet<string> ExcludedFolders = new(StringComparer.OrdinalIgnoreCase)
        {
            "node_modules", ".git", ".vs", ".vscode", "bin", "obj", 
            ".idea", "packages", ".nuget", "__pycache__", ".svn", 
            ".hg", "bower_components", "vendor", ".next", ".cache"
        };

        private static bool MatchesSearch(string fileName, string searchQuery)
        {
            // Support wildcard search with * and ?
            if (searchQuery.Contains('*') || searchQuery.Contains('?'))
            {
                try
                {
                    var pattern = "^" + System.Text.RegularExpressions.Regex.Escape(searchQuery)
                        .Replace("\\*", ".*")
                        .Replace("\\?", ".") + "$";
                    return System.Text.RegularExpressions.Regex.IsMatch(fileName, pattern, 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                catch
                {
                    return false;
                }
            }

            // Regular substring search
            return fileName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
        }

        private void SearchBreadthFirst(string rootPath, string searchQuery, System.Collections.Concurrent.ConcurrentBag<FileItem> results, int maxResults, System.Threading.CancellationToken cancellationToken)
        {
            var queue = new Queue<(string path, int depth)>();
            queue.Enqueue((rootPath, 0));

            while (queue.Count > 0 && results.Count < maxResults && !cancellationToken.IsCancellationRequested)
            {
                var (currentPath, depth) = queue.Dequeue();

                if (depth > 10) // Max depth limit
                    continue;

                try
                {
                    var dirInfo = new DirectoryInfo(currentPath);
                    
                    // Get all entries (files and directories) at once
                    var entries = dirInfo.GetFileSystemInfos();
                    
                    // Use parallel processing for better performance
                    var localMatches = new List<FileItem>();
                    var subdirs = new List<string>();

                    foreach (var entry in entries)
                    {
                        if (cancellationToken.IsCancellationRequested || results.Count >= maxResults)
                            break;

                        try
                        {
                            // Skip excluded system folders
                            if (entry is DirectoryInfo dir)
                            {
                                if (ExcludedFolders.Contains(dir.Name))
                                    continue;

                                subdirs.Add(dir.FullName);

                                if (MatchesSearch(dir.Name, searchQuery))
                                {
                                    localMatches.Add(new FileItem
                                    {
                                        Name = dir.Name,
                                        FullPath = dir.FullName,
                                        IsDirectory = true,
                                        Modified = dir.LastWriteTime,
                                        RelativePath = Path.GetRelativePath(rootPath, dir.FullName)
                                    });
                                }
                            }
                            else if (entry is FileInfo file)
                            {
                                if (MatchesSearch(file.Name, searchQuery))
                                {
                                    localMatches.Add(new FileItem
                                    {
                                        Name = file.Name,
                                        FullPath = file.FullName,
                                        IsDirectory = false,
                                        Size = file.Length,
                                        Modified = file.LastWriteTime,
                                        RelativePath = Path.GetRelativePath(rootPath, file.FullName)
                                    });
                                }
                            }
                        }
                        catch
                        {
                            // Skip entries we can't access
                        }
                    }

                    // Add matches to results
                    foreach (var match in localMatches)
                    {
                        if (results.Count >= maxResults)
                            break;
                        results.Add(match);
                    }

                    // Add subdirectories to queue for next level
                    foreach (var subdir in subdirs)
                    {
                        if (results.Count >= maxResults || cancellationToken.IsCancellationRequested)
                            break;
                        queue.Enqueue((subdir, depth + 1));
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip directories we don't have permission to access
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error searching directory {currentPath}: {ex.Message}");
                }
            }
        }
    }
}

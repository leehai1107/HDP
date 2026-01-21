using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Models;

namespace App.Services
{
    public interface IFileIndexService
    {
        Task<List<FileItem>> SearchIndexAsync(string searchQuery, CancellationToken cancellationToken = default);
        Task BuildIndexAsync(string rootPath, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
        Task RebuildIndexAsync(string rootPath, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
        bool IsIndexed { get; }
        int IndexedFileCount { get; }
        string IndexedPath { get; }
    }

    public class FileIndexService : IFileIndexService
    {
        private ConcurrentDictionary<string, FileItem> _fileIndex = new();
        private readonly object _indexLock = new object();
        private string _indexedPath = string.Empty;
        private static readonly HashSet<string> ExcludedFolders = new(StringComparer.OrdinalIgnoreCase)
        {
            "node_modules", ".git", ".vs", ".vscode", "bin", "obj",
            ".idea", "packages", ".nuget", "__pycache__", ".svn",
            ".hg", "bower_components", "vendor", ".next", ".cache",
            "$RECYCLE.BIN", "System Volume Information", "ProgramData",
            "Windows", "Program Files", "Program Files (x86)"
        };

        public bool IsIndexed => _fileIndex.Count > 0;
        public int IndexedFileCount => _fileIndex.Count;
        public string IndexedPath => _indexedPath;

        public async Task BuildIndexAsync(string rootPath, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            if (IsIndexed && _indexedPath == rootPath)
                return; // Already indexed

            await RebuildIndexAsync(rootPath, progress, cancellationToken);
        }

        public async Task RebuildIndexAsync(string rootPath, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            _indexedPath = rootPath;
            var newIndex = new ConcurrentDictionary<string, FileItem>();

            await Task.Run(() =>
            {
                try
                {
                    progress?.Report("Building file index...");
                    var stopwatch = Stopwatch.StartNew();
                    
                    IndexDirectoryParallel(rootPath, rootPath, newIndex, progress, cancellationToken);
                    
                    stopwatch.Stop();
                    progress?.Report($"Indexed {newIndex.Count:N0} files in {stopwatch.ElapsedMilliseconds}ms");
                }
                catch (OperationCanceledException)
                {
                    progress?.Report("Indexing cancelled");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error building index: {ex.Message}");
                    progress?.Report($"Error: {ex.Message}");
                }
            }, cancellationToken);

            lock (_indexLock)
            {
                _fileIndex = newIndex;
            }
        }

        private void IndexDirectoryParallel(string rootPath, string currentPath, ConcurrentDictionary<string, FileItem> index, IProgress<string>? progress, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            try
            {
                var dirInfo = new DirectoryInfo(currentPath);
                var entries = dirInfo.GetFileSystemInfos();
                var subdirectories = new List<string>();
                var itemCount = 0;

                // Process files and folders in parallel
                Parallel.ForEach(entries, new ParallelOptions 
                { 
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = cancellationToken 
                }, entry =>
                {
                    try
                    {
                        if (entry is DirectoryInfo dir)
                        {
                            if (ExcludedFolders.Contains(dir.Name))
                                return;

                            var relativePath = Path.GetRelativePath(rootPath, dir.FullName);
                            var item = new FileItem
                            {
                                Name = dir.Name,
                                FullPath = dir.FullName,
                                IsDirectory = true,
                                Modified = dir.LastWriteTime,
                                RelativePath = relativePath
                            };
                            
                            index.TryAdd(dir.FullName.ToLowerInvariant(), item);
                            
                            lock (subdirectories)
                            {
                                subdirectories.Add(dir.FullName);
                            }
                        }
                        else if (entry is FileInfo file)
                        {
                            var relativePath = Path.GetRelativePath(rootPath, file.FullName);
                            var item = new FileItem
                            {
                                Name = file.Name,
                                FullPath = file.FullName,
                                IsDirectory = false,
                                Size = file.Length,
                                Modified = file.LastWriteTime,
                                RelativePath = relativePath
                            };
                            
                            index.TryAdd(file.FullName.ToLowerInvariant(), item);
                        }

                        // Report progress every 5000 items to reduce overhead
                        var count = Interlocked.Increment(ref itemCount);
                        if (count % 5000 == 0)
                        {
                            progress?.Report($"Indexed {index.Count:N0} files...");
                        }
                    }
                    catch
                    {
                        // Skip entries we can't access
                    }
                });

                // Recursively index subdirectories in parallel
                Parallel.ForEach(subdirectories, new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount / 2, // Less parallelism for recursion
                    CancellationToken = cancellationToken
                }, subdir =>
                {
                    IndexDirectoryParallel(rootPath, subdir, index, progress, cancellationToken);
                });
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we don't have permission to access
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error indexing directory {currentPath}: {ex.Message}");
            }
        }

        public async Task<List<FileItem>> SearchIndexAsync(string searchQuery, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
                return new List<FileItem>();

            return await Task.Run(() =>
            {
                try
                {
                    var results = new List<FileItem>();
                    var maxResults = 1000;

                    // Parse search query for advanced syntax
                    var (query, filters) = ParseSearchQuery(searchQuery);

                    foreach (var kvp in _fileIndex)
                    {
                        if (cancellationToken.IsCancellationRequested || results.Count >= maxResults)
                            break;

                        var item = kvp.Value;

                        // Apply search match
                        if (!MatchesSearch(item, query))
                            continue;

                        // Apply filters
                        if (!ApplyFilters(item, filters))
                            continue;

                        results.Add(item);
                    }

                    // Sort: exact matches first, then by path depth, then alphabetically
                    return results
                        .OrderBy(x => !x.Name.Equals(query, StringComparison.OrdinalIgnoreCase)) // Exact match first
                        .ThenBy(x => x.RelativePath?.Split(Path.DirectorySeparatorChar).Length ?? 0) // Shallow first
                        .ThenBy(x => x.IsDirectory ? 0 : 1) // Folders first
                        .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error searching index: {ex.Message}");
                    return new List<FileItem>();
                }
            }, cancellationToken);
        }

        private (string query, Dictionary<string, string> filters) ParseSearchQuery(string searchQuery)
        {
            var filters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var query = searchQuery.Trim();

            // Support Everything-style filters: ext:, folder:, size:, etc.
            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var queryParts = new List<string>();

            foreach (var part in parts)
            {
                if (part.Contains(':'))
                {
                    var colonIndex = part.IndexOf(':');
                    var filterName = part.Substring(0, colonIndex);
                    var filterValue = part.Substring(colonIndex + 1);
                    filters[filterName] = filterValue;
                }
                else
                {
                    queryParts.Add(part);
                }
            }

            query = string.Join(" ", queryParts);
            return (query, filters);
        }

        private bool MatchesSearch(FileItem item, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            // Support wildcard search
            if (query.Contains('*') || query.Contains('?'))
            {
                try
                {
                    var pattern = "^" + System.Text.RegularExpressions.Regex.Escape(query)
                        .Replace("\\*", ".*")
                        .Replace("\\?", ".") + "$";
                    return System.Text.RegularExpressions.Regex.IsMatch(item.Name, pattern,
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                catch
                {
                    return false;
                }
            }

            // Fast substring search (case-insensitive)
            return item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                   (item.RelativePath?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        private bool ApplyFilters(FileItem item, Dictionary<string, string> filters)
        {
            if (filters.Count == 0)
                return true;

            foreach (var filter in filters)
            {
                switch (filter.Key.ToLowerInvariant())
                {
                    case "ext":
                    case "extension":
                        if (item.IsDirectory)
                            return false;
                        var ext = Path.GetExtension(item.Name).TrimStart('.');
                        if (!ext.Equals(filter.Value, StringComparison.OrdinalIgnoreCase))
                            return false;
                        break;

                    case "folder":
                    case "dir":
                        if (!item.IsDirectory)
                            return false;
                        break;

                    case "file":
                        if (item.IsDirectory)
                            return false;
                        break;

                    case "size":
                        // Simple size filter: >1MB, <500KB, etc.
                        if (!MatchesSizeFilter(item, filter.Value))
                            return false;
                        break;
                }
            }

            return true;
        }

        private bool MatchesSizeFilter(FileItem item, string sizeFilter)
        {
            if (item.IsDirectory)
                return false;

            try
            {
                var isGreater = sizeFilter.StartsWith('>');
                var isLess = sizeFilter.StartsWith('<');
                var sizeStr = sizeFilter.TrimStart('>', '<', '=');

                var multiplier = 1L;
                if (sizeStr.EndsWith("KB", StringComparison.OrdinalIgnoreCase))
                {
                    multiplier = 1024;
                    sizeStr = sizeStr.Substring(0, sizeStr.Length - 2);
                }
                else if (sizeStr.EndsWith("MB", StringComparison.OrdinalIgnoreCase))
                {
                    multiplier = 1024 * 1024;
                    sizeStr = sizeStr.Substring(0, sizeStr.Length - 2);
                }
                else if (sizeStr.EndsWith("GB", StringComparison.OrdinalIgnoreCase))
                {
                    multiplier = 1024 * 1024 * 1024;
                    sizeStr = sizeStr.Substring(0, sizeStr.Length - 2);
                }

                if (long.TryParse(sizeStr, out var targetSize))
                {
                    targetSize *= multiplier;
                    if (isGreater)
                        return item.Size > targetSize;
                    if (isLess)
                        return item.Size < targetSize;
                    return item.Size == targetSize;
                }
            }
            catch
            {
                // Invalid size filter
            }

            return true;
        }
    }
}

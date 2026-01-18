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
    }
}

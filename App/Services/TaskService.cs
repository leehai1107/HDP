using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using App.Models;

namespace App.Services
{
    public interface ITaskService
    {
        Task<List<TaskItem>> GetAllTasksAsync();
        Task<bool> AddTaskAsync(TaskItem task);
        Task<bool> UpdateTaskAsync(TaskItem task);
        Task<bool> DeleteTaskAsync(string taskId);
        Task SaveTasksAsync(List<TaskItem> tasks);
    }

    public class TaskService : ITaskService
    {
        private readonly string _tasksFilePath;

        public TaskService()
        {
            var appFolder = Path.Combine(AppContext.BaseDirectory, "Data");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            _tasksFilePath = Path.Combine(appFolder, "tasks.json");
        }

        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            try
            {
                if (!File.Exists(_tasksFilePath))
                {
                    return new List<TaskItem>();
                }

                var json = await File.ReadAllTextAsync(_tasksFilePath);
                var tasks = JsonSerializer.Deserialize<List<TaskItem>>(json);
                return tasks ?? new List<TaskItem>();
            }
            catch
            {
                return new List<TaskItem>();
            }
        }

        public async Task<bool> AddTaskAsync(TaskItem task)
        {
            try
            {
                var tasks = await GetAllTasksAsync();
                tasks.Add(task);
                await SaveTasksAsync(tasks);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateTaskAsync(TaskItem task)
        {
            try
            {
                var tasks = await GetAllTasksAsync();
                var existingTask = tasks.FirstOrDefault(t => t.Id == task.Id);
                if (existingTask != null)
                {
                    var index = tasks.IndexOf(existingTask);
                    tasks[index] = task;
                    await SaveTasksAsync(tasks);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteTaskAsync(string taskId)
        {
            try
            {
                var tasks = await GetAllTasksAsync();
                var task = tasks.FirstOrDefault(t => t.Id == taskId);
                if (task != null)
                {
                    tasks.Remove(task);
                    await SaveTasksAsync(tasks);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task SaveTasksAsync(List<TaskItem> tasks)
        {
            var json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_tasksFilePath, json);
        }
    }
}

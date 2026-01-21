using App.Models;
using App.ViewModels;
using Microsoft.Maui.Controls;

namespace App
{
    public partial class TasksPage : ContentPage
    {
        public TasksPage(FileExplorerViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is FileExplorerViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }

        private void OnTaskCheckBoxChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.BindingContext is TaskItem task && BindingContext is FileExplorerViewModel viewModel)
            {
                MainThread.BeginInvokeOnMainThread(async () => await viewModel.ToggleTaskDoneAsync(task));
            }
        }

        private async void OnNameDoubleTapped(object sender, TappedEventArgs e)
        {
            if (sender is Label label && label.BindingContext is TaskItem task)
            {
                string name = await DisplayPromptAsync("Edit Name", "Task Name:", initialValue: task.Name);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    task.Name = name;
                    
                    if (BindingContext is FileExplorerViewModel viewModel)
                    {
                        await viewModel.UpdateTaskAsync(task);
                    }
                }
            }
        }

        private async void OnDescriptionDoubleTapped(object sender, TappedEventArgs e)
        {
            if (sender is Label label && label.BindingContext is TaskItem task)
            {
                string description = await DisplayPromptAsync("Edit Description", "Description:", initialValue: task.Description);
                if (description != null)
                {
                    task.Description = description;
                    
                    if (BindingContext is FileExplorerViewModel viewModel)
                    {
                        await viewModel.UpdateTaskAsync(task);
                    }
                }
            }
        }

        private async void OnDateDoubleTapped(object sender, TappedEventArgs e)
        {
            if (sender is Label label && label.BindingContext is TaskItem task)
            {
                var result = await DisplayPromptAsync("Edit Due Date", "Enter date (MM/DD/YYYY):", initialValue: task.EndDate.ToString("MM/dd/yyyy"));
                if (!string.IsNullOrWhiteSpace(result))
                {
                    if (DateTime.TryParse(result, out DateTime newDate))
                    {
                        task.EndDate = newDate;
                        
                        if (BindingContext is FileExplorerViewModel viewModel)
                        {
                            await viewModel.UpdateTaskAsync(task);
                        }
                    }
                    else
                    {
                        await DisplayAlert("Invalid Date", "Please enter a valid date in MM/DD/YYYY format", "OK");
                    }
                }
            }
        }

        private async void OnStatusDoubleTapped(object sender, TappedEventArgs e)
        {
            if (sender is Label label && label.BindingContext is TaskItem task)
            {
                string status = await DisplayPromptAsync("Edit Status", "Status:", initialValue: task.Status);
                if (!string.IsNullOrWhiteSpace(status))
                {
                    task.Status = status;
                    
                    if (BindingContext is FileExplorerViewModel viewModel)
                    {
                        await viewModel.UpdateTaskAsync(task);
                    }
                }
            }
        }

        private async void OnCopyPathClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TaskItem task)
            {
                if (!string.IsNullOrWhiteSpace(task.AttachmentPath))
                {
                    await Clipboard.SetTextAsync(task.AttachmentPath);
                    await DisplayAlert("Copied", "Path copied to clipboard", "OK");
                }
            }
        }

        private void OnOpenFolderClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TaskItem task && BindingContext is FileExplorerViewModel viewModel)
            {
                if (!string.IsNullOrWhiteSpace(task.AttachmentPath))
                {
                    if (viewModel.NavigateToTaskPathCommand.CanExecute(task))
                    {
                        viewModel.NavigateToTaskPathCommand.Execute(task);
                    }
                }
            }
        }
    }
}

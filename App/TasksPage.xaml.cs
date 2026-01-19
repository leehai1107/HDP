using App.Models;
using App.ViewModels;
using Microsoft.Maui.Controls;

namespace App
{
    public partial class TasksPage : ContentPage
    {
        private TaskItem? _editingTask = null;

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

        private async void OnCreateTaskClicked(object sender, EventArgs e)
        {
            // Reset form fields
            if (BindingContext is FileExplorerViewModel viewModel)
            {
                viewModel.NewTaskName = string.Empty;
                viewModel.NewTaskDescription = string.Empty;
                viewModel.NewTaskEndDate = DateTime.Now.AddDays(1);
                viewModel.NewTaskAttachmentPath = string.Empty;
            }

            // Show modal
            await Navigation.PushModalAsync(new CreateTaskModal((FileExplorerViewModel)BindingContext));
        }

        private void OnTaskCheckBoxChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.BindingContext is TaskItem task && BindingContext is FileExplorerViewModel viewModel)
            {
                // Directly call the async method instead of using command to avoid UI freeze
                MainThread.BeginInvokeOnMainThread(async () => await viewModel.ToggleTaskDoneAsync(task));
            }
        }

        private void OnTaskDoubleTapped(object sender, TappedEventArgs e)
        {
            if (sender is Border border && border.BindingContext is TaskItem task)
            {
                _editingTask = task;
                
                // Find the edit controls within the border's content
                if (border.Content is Grid grid)
                {
                    // Find the hint label (row 4)
                    var hintLabel = grid.FindByName<Label>("DoubleClickHint");
                    var editNameEntry = grid.FindByName<Entry>("EditNameEntry");
                    var editDescriptionEditor = grid.FindByName<Editor>("EditDescriptionEditor");
                    var editButtonsGrid = grid.FindByName<Grid>("EditButtonsGrid");

                    if (hintLabel != null && editNameEntry != null && editDescriptionEditor != null && editButtonsGrid != null)
                    {
                        // Hide hint and show edit controls
                        hintLabel.IsVisible = false;
                        editNameEntry.IsVisible = true;
                        editDescriptionEditor.IsVisible = true;
                        editButtonsGrid.IsVisible = true;

                        // Focus on the name entry
                        editNameEntry.Focus();
                    }
                }
            }
        }

        private async void OnEditSaveClicked(object sender, EventArgs e)
        {
            if (_editingTask != null && BindingContext is FileExplorerViewModel viewModel)
            {
                if (sender is Button button && button.Parent is Grid buttonGrid && buttonGrid.Parent is Grid mainGrid)
                {
                    // Find entry and editor
                    var editNameEntry = mainGrid.FindByName<Entry>("EditNameEntry");
                    var editDescriptionEditor = mainGrid.FindByName<Editor>("EditDescriptionEditor");

                    if (editNameEntry != null && editDescriptionEditor != null)
                    {
                        _editingTask.Name = editNameEntry.Text ?? string.Empty;
                        _editingTask.Description = editDescriptionEditor.Text ?? string.Empty;

                        // Update task via service
                        await viewModel.UpdateTaskAsync(_editingTask);

                        // Hide edit controls
                        HideEditControls(mainGrid);
                    }
                }
            }
        }

        private void OnEditCancelClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.Parent is Grid buttonGrid && buttonGrid.Parent is Grid mainGrid)
            {
                HideEditControls(mainGrid);
            }

            _editingTask = null;
        }

        private void HideEditControls(Grid mainGrid)
        {
            var hintLabel = mainGrid.FindByName<Label>("DoubleClickHint");
            var editNameEntry = mainGrid.FindByName<Entry>("EditNameEntry");
            var editDescriptionEditor = mainGrid.FindByName<Editor>("EditDescriptionEditor");
            var editButtonsGrid = mainGrid.FindByName<Grid>("EditButtonsGrid");

            if (hintLabel != null && editNameEntry != null && editDescriptionEditor != null && editButtonsGrid != null)
            {
                hintLabel.IsVisible = true;
                editNameEntry.IsVisible = false;
                editDescriptionEditor.IsVisible = false;
                editButtonsGrid.IsVisible = false;
            }
        }
    }
}

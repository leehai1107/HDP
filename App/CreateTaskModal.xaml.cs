using App.ViewModels;
using Microsoft.Maui.Controls;

namespace App
{
    public partial class CreateTaskModal : ContentPage
    {
        private readonly FileExplorerViewModel _viewModel;

        public CreateTaskModal(FileExplorerViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        private async void OnBrowseFolderClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    FileTypes = null, // Allow all file types
                });

                if (result != null)
                {
                    // Get the parent directory of the selected file
                    var folderPath = Path.GetDirectoryName(result.FullPath) ?? result.FullPath;
                    _viewModel.NewTaskAttachmentPath = folderPath;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error selecting folder: {ex.Message}", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async void OnCreateTaskClickedModal(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_viewModel.NewTaskName))
            {
                await DisplayAlert("Validation", "Please enter a task name", "OK");
                TaskNameEntry.Focus();
                return;
            }

            // Create the task
            await _viewModel.AddTaskAsync();

            // Close the modal
            await Navigation.PopModalAsync();
        }
    }
}

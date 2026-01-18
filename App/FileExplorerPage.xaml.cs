using App.Models;
using App.ViewModels;
using Microsoft.Maui.Controls;

namespace App
{
    public partial class FileExplorerPage : ContentPage
    {
        public FileExplorerPage(FileExplorerViewModel viewModel)
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

        private void OnCollectionViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection?.FirstOrDefault() is FileItem selectedItem && BindingContext is FileExplorerViewModel viewModel)
            {
                // Deselect the item immediately to allow re-selection
                ((CollectionView)sender).SelectedItem = null;
                
                // Execute the navigate command
                if (viewModel.NavigateCommand.CanExecute(selectedItem))
                {
                    viewModel.NavigateCommand.Execute(selectedItem);
                }
            }
        }

        private void OnCheckBoxCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.BindingContext is FileItem item && BindingContext is FileExplorerViewModel viewModel)
            {
                // Manually sync the IsSelected property
                item.IsSelected = e.Value;
                
                // Update the SelectedItems collection
                if (e.Value && !viewModel.SelectedItems.Contains(item))
                {
                    viewModel.SelectedItems.Add(item);
                }
                else if (!e.Value && viewModel.SelectedItems.Contains(item))
                {
                    viewModel.SelectedItems.Remove(item);
                }
            }
        }

        private async void OnTaskCheckBoxChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.BindingContext is TaskItem task && BindingContext is FileExplorerViewModel viewModel)
            {
                // Directly call the async method instead of using command to avoid UI freeze
                await viewModel.ToggleTaskDoneAsync(task);
            }
        }
    }
}

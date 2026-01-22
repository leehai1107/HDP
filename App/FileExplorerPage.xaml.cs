using App.Models;
using App.ViewModels;
using App.Services;
using Microsoft.Maui.Controls;

namespace App
{
    public partial class FileExplorerPage : ContentPage
    {
        private readonly IAuthenticationService _authService;

        public FileExplorerPage(FileExplorerViewModel viewModel, IAuthenticationService authService)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _authService = authService;
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

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
            if (confirm)
            {
                _authService.Logout();
                await Shell.Current.GoToAsync("//Login");
            }
        }
    }
}

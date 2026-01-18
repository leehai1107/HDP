# File Explorer App - MAUI

A cross-platform file explorer application built with .NET MAUI that allows you to browse, manage, and organize files and folders.

## Features

- 📁 Browse directories and files
- 🔄 Navigate back to parent directories
- ➕ Create new folders
- 🗑️ Delete files and folders
- 📊 View file size and modification date
- 🌙 Dark/Light theme support
- ⚙️ Configurable root path via `appsettings.json`

## Configuration

The app uses an `appsettings.json` file to configure the root directory path.

### appsettings.json

```json
{
  "FileExplorer": {
    "RootPath": "C:/Users"
  }
}
```

**RootPath**: The initial directory path where the file explorer will start. If not specified or invalid, it defaults to the user's home directory.

### Changing the Root Path

Edit the `appsettings.json` file and set the `RootPath` to your desired directory:

```json
{
  "FileExplorer": {
    "RootPath": "D:/MyDocuments"
  }
}
```

## Project Structure

```
App/
├── appsettings.json              # Configuration file
├── Models/
│   └── FileItem.cs              # File/folder model
├── Services/
│   ├── FileExplorerService.cs   # File operations
│   └── ConfigurationService.cs  # Configuration management
├── ViewModels/
│   └── FileExplorerViewModel.cs # MVVM ViewModel
├── Converters/
│   └── IntToBoolConverter.cs    # Value converter for UI
├── FileExplorerPage.xaml        # File explorer UI
├── FileExplorerPage.xaml.cs     # Code-behind
└── MauiProgram.cs              # App initialization
```

## Usage

### Basic Navigation

1. **Launch the app** - Opens at the configured root path
2. **Tap folders** - Double-click to enter a directory
3. **Back button** - Navigate to the parent directory
4. **Refresh button** - Reload the current directory

### Create Folders

1. Enter a folder name in the text field at the top
2. Tap the "➕ Create" button
3. New folder is created in the current directory

### Delete Items

1. Tap the "🗑️" button next to any file or folder
2. Confirm the deletion when prompted
3. The item is removed immediately

### View Details

Each item displays:

- **Icon**: 📁 for folders, 📄 for files
- **Name**: File or folder name
- **Modified Date**: Last modification date and time
- **Size**: File size (folders show "-")

## Architecture

### MVVM Pattern

The app uses the MVVM (Model-View-ViewModel) architecture:

- **Model**: `FileItem` class represents files and folders
- **ViewModel**: `FileExplorerViewModel` handles all logic and state
- **View**: XAML pages bind to the ViewModel

### Services

1. **IFileExplorerService** - Handles all file system operations

   - Get files and folders
   - Create, delete operations
   - Path validation

2. **IConfigurationService** - Manages app configuration
   - Loads settings from `appsettings.json`
   - Provides root path configuration

## Permissions

The app requires file system access permissions:

- **Windows**: Standard file access
- **Android**: `READ_EXTERNAL_STORAGE`, `WRITE_EXTERNAL_STORAGE`
- **iOS**: Document permissions
- **macOS**: File system access

## Building and Running

### Prerequisites

- .NET 9.0 SDK
- MAUI workload installed

### Build

```bash
dotnet build
```

### Run on Windows

```bash
dotnet run -f net9.0-windows10.0.19041.0
```

### Run on Android

```bash
dotnet run -f net9.0-android
```

### Run on iOS

```bash
dotnet run -f net9.0-ios
```

## Customization

### Change Root Path

Edit `appsettings.json`:

```json
{
  "FileExplorer": {
    "RootPath": "/your/path/here"
  }
}
```

### Modify Colors

Edit `FileExplorerPage.xaml` to change button colors and theme:

```xaml
<Button BackgroundColor="#2196F3" TextColor="White" />
```

### Add More Features

To extend the app:

1. Add new methods to `FileExplorerService`
2. Add corresponding commands to `FileExplorerViewModel`
3. Update the XAML UI accordingly

## Known Limitations

- Cannot access directories without read permissions
- File operations may fail on some platforms due to security restrictions
- Very large directories may be slow to load

## Error Handling

The app handles common errors gracefully:

- Permission denied errors are skipped silently
- Failed operations show user-friendly error messages
- Invalid paths default to the user's home directory

## Future Enhancements

- [ ] File preview/thumbnails
- [ ] Search functionality
- [ ] File context menu options
- [ ] Drag and drop support
- [ ] Multi-select operations
- [ ] Recent files history
- [ ] Favorites/bookmarks

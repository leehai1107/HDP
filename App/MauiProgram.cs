using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using App.Services;
using App.ViewModels;

namespace App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            
            // Load configuration
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register services
            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
            builder.Services.AddSingleton<IFileExplorerService, FileExplorerService>();
            builder.Services.AddSingleton<IFileIndexService, FileIndexService>();
            builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
            builder.Services.AddSingleton<ITaskService, TaskService>();
            builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

            // Register ViewModels
            builder.Services.AddSingleton<FileExplorerViewModel>();

            // Register Pages
            builder.Services.AddSingleton<FileExplorerPage>();
            builder.Services.AddSingleton<TasksPage>();
            builder.Services.AddSingleton<CreateTaskModal>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<UserManagementPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

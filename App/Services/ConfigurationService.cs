using System;
using Microsoft.Extensions.Configuration;

namespace App.Services
{
    public interface IConfigurationService
    {
        string GetRootPath();
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;

        public ConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetRootPath()
        {
            var rootPath = _configuration["FileExplorer:RootPath"];
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            return rootPath;
        }
    }
}

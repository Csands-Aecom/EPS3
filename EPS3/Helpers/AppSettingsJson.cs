// Requires NuGet package Microsoft.Extensions.Configuration.Json

using Microsoft.Extensions.Configuration;
using System.IO;

namespace EPS3.Helpers
{
    public static class AppSettingsJson
    {
        public static string ApplicationExeDirectory()
        {
            var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var appRoot = Path.GetDirectoryName(location);

            return appRoot;
        }

        public static IConfigurationRoot GetAppSettings()
        {
            string applicationExeDirectory = ApplicationExeDirectory();

            var builder = new ConfigurationBuilder()
            .SetBasePath(applicationExeDirectory)
            .AddJsonFile("appsettings.json");

            return builder.Build();
        }

        private static string _userFilesPhysicalPath = "";

        public static string UserFilesPhysicalPath()
        {
            if (string.IsNullOrEmpty(_userFilesPhysicalPath))
            {
                var appSettingsJson = AppSettingsJson.GetAppSettings();
                var UserFilesPhysicalPathSetting = appSettingsJson["UserFilesPhysicalPath"];
                if (UserFilesPhysicalPathSetting.StartsWith("{wwwroot}"))
                {
                    _userFilesPhysicalPath = UserFilesPhysicalPathSetting.Replace("{wwwroot}", ApplicationExeDirectory() + "\\wwwroot");
                }
            }
            return _userFilesPhysicalPath;

        }

    }
}
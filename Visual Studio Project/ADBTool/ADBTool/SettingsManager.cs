using Newtonsoft.Json;
using System;
using System.IO;

namespace ADBTool
{
    public static class SettingsManager
    {

        private static Settings _settings = null;
        public static Settings Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = new Settings();
                }

                return _settings;
            }
        }

        private readonly static string _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private const string SETTINGS_FOLDER = "ADBTool";
        private const string SETTINGS_FILENAME = "settings.ini";

        private static string GetSettingsPath()
        {
            string folderPath = Path.Combine(_appDataPath, SETTINGS_FOLDER);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            return Path.Combine(folderPath, SETTINGS_FILENAME);
        }

        public static void SaveSettings()
        {
            string settingsAsString = JsonConvert.SerializeObject(Settings);

            File.WriteAllText(GetSettingsPath(), settingsAsString);
        }

        public static void LoadSettings()
        {
            string path = GetSettingsPath();

            if (!File.Exists(path))
            {
                //_settings = new Settings();
                return;
            }

            string settingsAsString = File.ReadAllText(path);

            _settings = JsonConvert.DeserializeObject<Settings>(settingsAsString);
        }
    }
}

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Ai.SolidWorksAssistant.Settings
{
    [DataContract]
    public sealed class AppSettings
    {
        /// <summary>"auto" (follow Windows UI language), "fa" or "en".</summary>
        [DataMember(Name = "language")]
        public string Language = "auto";

        /// <summary>Show the CommandManager tab/button (best-effort reopen path).</summary>
        [DataMember(Name = "showCommandTab")]
        public bool ShowCommandTab = true;

        /// <summary>Limit the feature-tree summary length in the context card.</summary>
        [DataMember(Name = "maxFeaturesInContext")]
        public int MaxFeaturesInContext = 8;
    }

    /// <summary>
    /// Loads/saves settings.json under %APPDATA%\AiSolidWorksAssistant.
    /// Uses DataContractJsonSerializer — zero external dependencies.
    /// A missing or corrupt file always falls back to defaults (never blocks add-in load).
    /// </summary>
    public sealed class JsonSettingsStore
    {
        private readonly string _path;

        public JsonSettingsStore()
            : this(DefaultPath())
        {
        }

        public JsonSettingsStore(string path)
        {
            _path = string.IsNullOrEmpty(path) ? DefaultPath() : path;
        }

        public static string DefaultPath()
        {
            return System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiSolidWorksAssistant",
                "settings.json");
        }

        public string Path
        {
            get { return _path; }
        }

        public AppSettings Load()
        {
            try
            {
                if (!File.Exists(_path))
                {
                    return new AppSettings();
                }

                var json = File.ReadAllText(_path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new AppSettings();
                }

                var serializer = new DataContractJsonSerializer(typeof(AppSettings));
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var settings = (AppSettings)serializer.ReadObject(stream);
                    return settings ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Core.Log.Warn("Settings load failed, using defaults: " + ex.Message);
                return new AppSettings();
            }
        }

        public bool Save(AppSettings settings)
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var serializer = new DataContractJsonSerializer(typeof(AppSettings));
                using (var stream = new MemoryStream())
                {
                    serializer.WriteObject(stream, settings);
                    File.WriteAllText(_path, Encoding.UTF8.GetString(stream.ToArray()), Encoding.UTF8);
                }
                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Warn("Settings save failed: " + ex.Message);
                return false;
            }
        }
    }
}

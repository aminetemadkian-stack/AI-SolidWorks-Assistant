using System;
using System.IO;
using Ai.SolidWorksAssistant.Settings;
using Xunit;

namespace Ai.SolidWorksAssistant.Tests
{
    public class JsonSettingsStoreTests : IDisposable
    {
        private readonly string _path;

        public JsonSettingsStoreTests()
        {
            _path = Path.Combine(Path.GetTempPath(), "aiswa-tests-" + Guid.NewGuid().ToString("N") + ".json");
        }

        public void Dispose()
        {
            try
            {
                File.Delete(_path);
            }
            catch
            {
            }
        }

        [Fact]
        public void Missing_file_returns_defaults()
        {
            var store = new JsonSettingsStore(_path);
            var settings = store.Load();

            Assert.Equal("auto", settings.Language);
            Assert.True(settings.ShowCommandTab);
            Assert.Equal(8, settings.MaxFeaturesInContext);
        }

        [Fact]
        public void Round_trip_preserves_values()
        {
            var store = new JsonSettingsStore(_path);
            var original = new AppSettings
            {
                Language = "fa",
                ShowCommandTab = false,
                MaxFeaturesInContext = 12
            };

            Assert.True(store.Save(original));

            var loaded = new JsonSettingsStore(_path).Load();
            Assert.Equal("fa", loaded.Language);
            Assert.False(loaded.ShowCommandTab);
            Assert.Equal(12, loaded.MaxFeaturesInContext);
        }

        [Fact]
        public void Corrupt_file_falls_back_to_defaults()
        {
            File.WriteAllText(_path, "{ this is not json");
            var store = new JsonSettingsStore(_path);
            var settings = store.Load();

            Assert.NotNull(settings);
            Assert.Equal("auto", settings.Language);
        }
    }
}

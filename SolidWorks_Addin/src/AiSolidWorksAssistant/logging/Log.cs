using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Ai.SolidWorksAssistant.Core
{
    /// <summary>
    /// Minimal file logger. No-op until Initialize() is called, so unit tests stay side-effect free.
    /// Logs to %APPDATA%\AiSolidWorksAssistant\logs\addin-YYYYMMDD.log by default.
    /// </summary>
    public static class Log
    {
        private static readonly object Gate = new object();
        private static string _directory;
        private static string _fileName = "addin";

        public static event Action<string> Sink;

        public static string DefaultDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AiSolidWorksAssistant",
                    "logs");
            }
        }

        public static void Initialize(string directory = null, string fileName = null)
        {
            lock (Gate)
            {
                _directory = string.IsNullOrEmpty(directory) ? DefaultDirectory : directory;
                if (!string.IsNullOrEmpty(fileName))
                {
                    _fileName = fileName;
                }
                Directory.CreateDirectory(_directory);
            }
        }

        public static void Info(string message)
        {
            Write("INFO ", message);
        }

        public static void Warn(string message)
        {
            Write("WARN ", message);
        }

        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        public static void Error(string message, Exception ex)
        {
            Write("ERROR", message + " :: " + ex);
        }

        private static void Write(string level, string message)
        {
            try
            {
                var line = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}",
                    DateTime.Now, level, message);

                Sink?.Invoke(line);

                lock (Gate)
                {
                    if (_directory == null)
                    {
                        return;
                    }
                    Directory.CreateDirectory(_directory);
                    var path = Path.Combine(
                        _directory,
                        _fileName + "-" + DateTime.Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture) + ".log");
                    File.AppendAllText(path, line + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AiSWA log failure: " + ex.Message);
            }
        }
    }
}

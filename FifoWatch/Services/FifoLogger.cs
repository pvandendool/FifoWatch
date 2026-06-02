using System;
using System.Collections.Generic;
using System.IO;
using FifoWatch.Models;

namespace FifoWatch.Services
{
    internal static class FifoLogger
    {
        public static readonly string DefaultLogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FifoWatch", "logs");

        // Returns null on success, or an error message on failure.
        public static string Append(string logFolder, string monitorName, List<FifoEntry> entries)
        {
            try
            {
                string dir = string.IsNullOrWhiteSpace(logFolder) ? DefaultLogDir : logFolder;
                Directory.CreateDirectory(dir);
                string safeName = string.Concat(monitorName.Split(Path.GetInvalidFileNameChars()));
                string path = Path.Combine(dir, safeName + ".log");
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                using (var w = new StreamWriter(path, append: true))
                {
                    foreach (var entry in entries)
                        w.WriteLine($"{timestamp}  {entry.Variable}  {entry.Value}");
                }
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}

using System;
using System.IO;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.Utility
{
    /// <summary>
    /// Saves runtime screenshots to a stable folder for debug sharing.
    /// </summary>
    public static class RuntimeScreenshotCapture
    {
        public const string DefaultDirectoryName = "Screenshots";
        public const string LatestFileName = "latest.png";

        public static string Capture(string prefix = "screenshot", int superSize = 1)
        {
            string directoryPath = GetScreenshotDirectoryPath();
            Directory.CreateDirectory(directoryPath);

            string safePrefix = MakeSafeFileNamePrefix(prefix);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filePath = Path.Combine(directoryPath, $"{safePrefix}_{timestamp}.png");

            ScreenCapture.CaptureScreenshot(filePath, Mathf.Max(1, superSize));
            CopyToLatestWhenReady(filePath, Path.Combine(directoryPath, LatestFileName));

            Debug.Log($"[RuntimeScreenshot] Capture requested: {filePath}");
            return filePath;
        }

        public static string GetScreenshotDirectoryPath()
        {
            return Path.Combine(Application.persistentDataPath, DefaultDirectoryName);
        }

        private static string MakeSafeFileNamePrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return "screenshot";
            }

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                prefix = prefix.Replace(invalidChar, '_');
            }

            return prefix.Trim();
        }

        private static async void CopyToLatestWhenReady(string sourcePath, string latestPath)
        {
            const int maxAttempts = 30;
            const int delayMilliseconds = 100;

            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    if (File.Exists(sourcePath) && new FileInfo(sourcePath).Length > 0)
                    {
                        File.Copy(sourcePath, latestPath, true);
                        Debug.Log($"[RuntimeScreenshot] Latest updated: {latestPath}");
                        return;
                    }
                }
                catch (IOException)
                {
                    // Unity may still be writing the screenshot; retry briefly.
                }
                catch (UnauthorizedAccessException ex)
                {
                    Debug.LogWarning($"[RuntimeScreenshot] Failed to update latest screenshot: {ex.Message}");
                    return;
                }

                await System.Threading.Tasks.Task.Delay(delayMilliseconds);
            }

            Debug.LogWarning($"[RuntimeScreenshot] Timed out waiting for screenshot file: {sourcePath}");
        }
    }
}

using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.Utility
{
    /// <summary>
    /// ランタイム中の画面を Texture / バイト列 / ファイル保存の各段階で扱うための共通スクリーンショット API です。
    /// ゲーム内プレビュー、アルバム、共有前確認、デバッグ保存など、用途に応じて必要な段階だけを使えます。
    /// </summary>
    public static class RuntimeScreenshotCapture
    {
        public const string DefaultDirectoryName = "Screenshots";
        public const string LatestFileName = "latest.png";

        private const string DefaultPrefix = "screenshot";
        private const int DefaultJpegQuality = 90;

        private static ScreenshotCoroutineRunner _runner;

        /// <summary>
        /// 従来互換の簡易 API です。
        /// Unity 標準の ScreenCapture.CaptureScreenshot を使い、指定ファイルへの書き込みを Unity 側へ依頼します。
        /// 画面をゲーム内で Texture として使いたい場合は CaptureTextureAsync を使ってください。
        /// </summary>
        public static string Capture(string prefix = "screenshot", int superSize = 1)
        {
            string directoryPath = GetScreenshotDirectoryPath();
            string filePath = BuildTimestampedFilePath(directoryPath, prefix, "png");

            ScreenCapture.CaptureScreenshot(filePath, Mathf.Max(1, superSize));
            CopyToLatestWhenReady(filePath, Path.Combine(directoryPath, LatestFileName));

            Debug.Log($"[RuntimeScreenshot] Capture requested: {filePath}");
            return filePath;
        }

        /// <summary>
        /// 現在の画面を Texture2D として取得します。
        /// 返された Texture2D は RawImage 表示、演出素材、ゲーム内アルバムなどにそのまま使えます。
        /// 使い終わった Texture2D は呼び出し側で Destroy してください。
        /// </summary>
        public static Task<Texture2D> CaptureTextureAsync(Rect? sourceRect = null, bool includeAlpha = false)
        {
            var completion = new TaskCompletionSource<Texture2D>();
            GetOrCreateRunner().StartCoroutine(CaptureTextureCoroutine(sourceRect, includeAlpha, completion));
            return completion.Task;
        }

        /// <summary>
        /// 現在の画面を撮影し、PNG の byte[] まで変換します。
        /// ファイル保存せずにアップロード、共有、独自保存処理へ渡したい場合に使います。
        /// </summary>
        public static async Task<byte[]> CapturePngBytesAsync(Rect? sourceRect = null, bool includeAlpha = false)
        {
            Texture2D texture = await CaptureTextureAsync(sourceRect, includeAlpha);
            try
            {
                return EncodePng(texture);
            }
            finally
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        /// <summary>
        /// 現在の画面を撮影し、JPG の byte[] まで変換します。
        /// 透過が不要で、PNG よりファイルサイズを抑えたい場合に使います。
        /// </summary>
        public static async Task<byte[]> CaptureJpgBytesAsync(Rect? sourceRect = null, int quality = DefaultJpegQuality)
        {
            Texture2D texture = await CaptureTextureAsync(sourceRect, false);
            try
            {
                return EncodeJpg(texture, quality);
            }
            finally
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        /// <summary>
        /// Texture2D を PNG バイト列に変換します。
        /// 撮影後に加工した Texture を保存・共有したい場合は、この段階だけを単独で使えます。
        /// </summary>
        public static byte[] EncodePng(Texture2D texture)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            return texture.EncodeToPNG();
        }

        /// <summary>
        /// Texture2D を JPG バイト列に変換します。
        /// quality は 1-100 に丸められます。
        /// </summary>
        public static byte[] EncodeJpg(Texture2D texture, int quality = DefaultJpegQuality)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            return texture.EncodeToJPG(Mathf.Clamp(quality, 1, 100));
        }

        /// <summary>
        /// byte[] を指定パスへ保存します。
        /// PNG/JPG 以外の独自形式でも、呼び出し側で作ったバイト列をそのまま保存できます。
        /// </summary>
        public static string SaveBytes(byte[] bytes, string filePath, bool updateLatest = false)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("保存先パスが空です。", nameof(filePath));
            }

            string directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllBytes(filePath, bytes);

            if (updateLatest && !string.IsNullOrWhiteSpace(directoryPath))
            {
                File.Copy(filePath, Path.Combine(directoryPath, LatestFileName), true);
            }

            Debug.Log($"[RuntimeScreenshot] Saved: {filePath}");
            return filePath;
        }

        /// <summary>
        /// 画面撮影から PNG 保存までを一度に行う便利 API です。
        /// 内部では CaptureTextureAsync -> EncodePng -> SaveBytes の順で処理するため、
        /// ゲーム内で使う Texture 取得処理と同じ経路で保存できます。
        /// </summary>
        public static async Task<string> CaptureAndSavePngAsync(
            string prefix = DefaultPrefix,
            string directoryPath = null,
            Rect? sourceRect = null,
            bool includeAlpha = false,
            bool updateLatest = true)
        {
            directoryPath = string.IsNullOrWhiteSpace(directoryPath) ? GetScreenshotDirectoryPath() : directoryPath;
            string filePath = BuildTimestampedFilePath(directoryPath, prefix, "png");
            byte[] bytes = await CapturePngBytesAsync(sourceRect, includeAlpha);
            return SaveBytes(bytes, filePath, updateLatest);
        }

        /// <summary>
        /// 画面撮影から JPG 保存までを一度に行う便利 API です。
        /// 透過不要で、保存容量を抑えたいスクリーンショットに向いています。
        /// </summary>
        public static async Task<string> CaptureAndSaveJpgAsync(
            string prefix = DefaultPrefix,
            string directoryPath = null,
            Rect? sourceRect = null,
            int quality = DefaultJpegQuality,
            bool updateLatest = true)
        {
            directoryPath = string.IsNullOrWhiteSpace(directoryPath) ? GetScreenshotDirectoryPath() : directoryPath;
            string filePath = BuildTimestampedFilePath(directoryPath, prefix, "jpg");
            byte[] bytes = await CaptureJpgBytesAsync(sourceRect, quality);
            return SaveBytes(bytes, filePath, updateLatest);
        }

        /// <summary>
        /// デフォルトのスクリーンショット保存先です。
        /// ランタイムビルドでも使えるように Application.persistentDataPath 配下へ保存します。
        /// </summary>
        public static string GetScreenshotDirectoryPath()
        {
            return Path.Combine(Application.persistentDataPath, DefaultDirectoryName);
        }

        /// <summary>
        /// タイムスタンプ付きの安全なファイルパスを作ります。
        /// ファイル名に使えない文字は '_' に置き換えます。
        /// </summary>
        public static string BuildTimestampedFilePath(string directoryPath, string prefix = DefaultPrefix, string extension = "png")
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                directoryPath = GetScreenshotDirectoryPath();
            }

            Directory.CreateDirectory(directoryPath);

            string safePrefix = MakeSafeFileNamePrefix(prefix);
            string safeExtension = MakeSafeExtension(extension);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(directoryPath, $"{safePrefix}_{timestamp}.{safeExtension}");
        }

        private static string MakeSafeFileNamePrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return DefaultPrefix;
            }

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                prefix = prefix.Replace(invalidChar, '_');
            }

            return prefix.Trim();
        }

        private static string MakeSafeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return "png";
            }

            extension = extension.Trim().TrimStart('.');
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                extension = extension.Replace(invalidChar, '_');
            }

            return string.IsNullOrWhiteSpace(extension) ? "png" : extension;
        }

        private static IEnumerator CaptureTextureCoroutine(Rect? sourceRect, bool includeAlpha, TaskCompletionSource<Texture2D> completion)
        {
            // 画面の描画が完了してから ReadPixels することで、UI を含む最終表示を取得します。
            yield return new WaitForEndOfFrame();

            try
            {
                Rect rect = sourceRect ?? new Rect(0f, 0f, Screen.width, Screen.height);
                int width = Mathf.Max(1, Mathf.RoundToInt(rect.width));
                int height = Mathf.Max(1, Mathf.RoundToInt(rect.height));
                TextureFormat format = includeAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24;
                var texture = new Texture2D(width, height, format, false);

                texture.ReadPixels(rect, 0, 0);
                texture.Apply(false, false);
                completion.TrySetResult(texture);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        }

        private static ScreenshotCoroutineRunner GetOrCreateRunner()
        {
            if (_runner != null)
            {
                return _runner;
            }

            var runnerObject = new GameObject("[EightAID Runtime Screenshot Capture]");
            runnerObject.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(runnerObject);
            _runner = runnerObject.AddComponent<ScreenshotCoroutineRunner>();
            return _runner;
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

        private sealed class ScreenshotCoroutineRunner : MonoBehaviour
        {
        }
    }
}

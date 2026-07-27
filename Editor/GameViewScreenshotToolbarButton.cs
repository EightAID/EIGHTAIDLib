using System;
using System.IO;
using EightAID.EIGHTAIDLib.Utility;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using YujiAp.UnityToolbarExtension.Editor;

namespace EightAID.EIGHTAIDLib.Editor
{
    /// <summary>
    /// Unity Editor の上部ツールバーから、現在の GameView 表示を PNG として保存するためのボタンです。
    /// RuntimeScreenshotCapture の汎用パイプラインを使うため、ゲーム内で使う撮影経路と同じ挙動を確認できます。
    /// </summary>
    public sealed class GameViewScreenshotToolbarButton : IToolbarElement
    {
        private const string ButtonName = "EightAIDGameViewScreenshotButton";
        private const string ButtonText = "GameView Shot";
        private const string SaveFolderName = "Screenshots";

        public ToolbarElementLayoutType DefaultLayoutType => ToolbarElementLayoutType.RightSideRightAlign;

        public VisualElement CreateElement()
        {
            var button = new EditorToolbarButton
            {
                name = ButtonName,
                text = ButtonText,
                tooltip = "現在の GameView を PNG として保存し、保存先フォルダを開きます。"
            };

            button.clicked += () => CaptureCurrentGameView(button);
            return button;
        }

        private static async void CaptureCurrentGameView(EditorToolbarButton button)
        {
            if (!Application.isPlaying)
            {
                // RuntimeScreenshotCapture は WaitForEndOfFrame 後の ReadPixels で画面を読むため、
                // GameView の実行中表示がある Play Mode で使う前提にします。
                EditorUtility.DisplayDialog(
                    "GameView Screenshot",
                    "GameView のスクリーンショットは Play Mode 中に実行してください。",
                    "OK");
                return;
            }

            string previousText = button.text;
            button.SetEnabled(false);
            button.text = "Saving...";

            try
            {
                string directoryPath = GetEditorScreenshotDirectoryPath();

                // GameView の最終描画を Texture2D として取得し、PNG 化して保存します。
                // Play Mode 中の UI プレビューや、Editor 上での確認画像作成に使いやすい導線です。
                string filePath = await RuntimeScreenshotCapture.CaptureAndSavePngAsync(
                    "gameview",
                    directoryPath,
                    updateLatest: true);

                Debug.Log($"[EightAID GameView Screenshot] Saved: {filePath}");
                EditorUtility.RevealInFinder(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EightAID GameView Screenshot] Failed: {ex}");
                EditorUtility.DisplayDialog(
                    "GameView Screenshot",
                    $"GameView のスクリーンショット保存に失敗しました。\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                button.text = previousText;
                button.SetEnabled(true);
            }
        }

        private static string GetEditorScreenshotDirectoryPath()
        {
            // Editor の便利保存先はプロジェクト内の Recordings 配下にします。
            // persistentDataPath より見つけやすく、Git 管理外の一時成果物として扱いやすい場所です。
            return Path.Combine(Application.dataPath, "..", "Recordings", SaveFolderName);
        }
    }
}

using System.IO;
using Roguelike.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelike.Editor
{
    public static class BattlePreviewCapture
    {
        // Batch-only preview: edits live in memory and are never saved to the scene.
        public static void Capture()
        {
            EditorSceneManager.OpenScene(BattleSceneTools.ScenePath);
            Object.FindFirstObjectByType<BattleSetup>().InitializeBattle();
            var camera = Camera.main;
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var target = new RenderTexture(1280, 720, 24);
            var previous = RenderTexture.active;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                canvas.GetComponent<CanvasScaler>().enabled = false;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                canvas.scaleFactor = 1;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/BattlePreview.png"));
                File.WriteAllBytes(path, image.EncodeToPNG());
                Debug.Log("BATTLE_PREVIEW_SAVED: " + path);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }
    }
}

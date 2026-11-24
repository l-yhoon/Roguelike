using System.IO;
using Roguelike.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Roguelike.UI;
using System;
using Object = UnityEngine.Object;

namespace Roguelike.Editor
{
    public static class BattlePreviewCapture
    {
        // Batch-only preview: edits live in memory and are never saved to the scene.
        public static void Capture()
        {
            CaptureAtSize(1280, 720, "BattlePreview.png");
        }

        public static void CaptureAtSize(int width, int height, string filename, Action<Canvas> validate = null)
        {
            EditorSceneManager.OpenScene(BattleSceneTools.ScenePath);
            Object.FindFirstObjectByType<BattleSetup>().InitializeBattle();
            var hud = Object.FindFirstObjectByType<BattleHud>();
            if (hud != null) hud.BindBattle();
            var camera = Camera.main;
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var target = new RenderTexture(width, height, 24);
            var previous = RenderTexture.active;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                var scaler = canvas.GetComponent<CanvasScaler>();
                float widthScale = width / scaler.referenceResolution.x;
                float heightScale = height / scaler.referenceResolution.y;
                float scale;
                switch (scaler.screenMatchMode)
                {
                    case CanvasScaler.ScreenMatchMode.Expand: scale = Mathf.Min(widthScale, heightScale); break;
                    case CanvasScaler.ScreenMatchMode.Shrink: scale = Mathf.Max(widthScale, heightScale); break;
                    default:
                        scale = Mathf.Pow(2, Mathf.Lerp(Mathf.Log(widthScale, 2), Mathf.Log(heightScale, 2), scaler.matchWidthOrHeight));
                        break;
                }
                // A RenderTexture does not change Screen.width/height. Reproduce the
                // configured CanvasScaler calculation for the requested capture size.
                scaler.enabled = false;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                canvas.scaleFactor = scale;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
                Canvas.ForceUpdateCanvases();
                Object.FindFirstObjectByType<EnemyIntentView>().RefreshPosition();
                validate?.Invoke(canvas);
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/" + filename));
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

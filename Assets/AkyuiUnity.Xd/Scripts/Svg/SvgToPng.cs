using System.IO;
using AkyuiUnity.Editor.Extensions;
using Unity.VectorGraphics;
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEngine;

namespace AkyuiUnity.Xd
{
    public static class SvgToPng
    {
        public static VectorUtils.TessellationOptions TessellationOptions => new VectorUtils.TessellationOptions
        {
            StepDistance = 100.0f,
            MaxCordDeviation = 0.5f,
            MaxTanAngleDeviation = 0.1f,
            SamplingStepSize = 0.01f
        };

        public static byte[] Convert(string svg, Vector2 size)
        {
            var unityAssetsParentPath = Path.GetDirectoryName(Application.dataPath) ?? "";
            var savePath = Path.Combine("Assets", "Temp.svg");
            var saveFullPath = Path.Combine(unityAssetsParentPath, savePath);

            SvgImportTrigger.ProcessingFile = savePath;
            var spriteSaveScale = XdImporter.Settings.SpriteSaveScale;
            SvgImportTrigger.Size = new Vector2Int(Mathf.RoundToInt(size.x * spriteSaveScale), Mathf.RoundToInt(size.y * spriteSaveScale));

            using (Disposable.Create(() =>
            {
                SvgImportTrigger.ProcessingFile = null;
                SvgImportTrigger.Size = Vector2Int.zero;
                AssetDatabase.DeleteAsset(savePath);
            }))
            {
                File.WriteAllBytes(saveFullPath, System.Text.Encoding.UTF8.GetBytes(svg));
                AssetDatabase.ImportAsset(savePath);
                // SvgImportTrigger.OnPreprocessAssetが呼ばれる
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(savePath);
                return texture.EncodeToPNG();
            }
        }
    }

    public class SvgImportTrigger : AssetPostprocessor
    {
        public static string ProcessingFile { get; set; }
        public static Vector2Int Size { get; set; }

        public void OnPreprocessAsset()
        {
            if (ProcessingFile != assetPath) return;
            if (!(assetImporter is SVGImporter svgImporter)) return;

            svgImporter.SvgType = SVGType.TexturedSprite;
            svgImporter.KeepTextureAspectRatio = false;
            svgImporter.TextureWidth = Size.x;
            svgImporter.TextureHeight = Size.y;
        }
    }
}
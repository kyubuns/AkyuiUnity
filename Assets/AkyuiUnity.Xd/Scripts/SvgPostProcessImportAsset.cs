using AkyuiUnity.Editor;
using Newtonsoft.Json;
using Unity.VectorGraphics.Editor;
using UnityEditor;

namespace AkyuiUnity.Xd
{
    public class SvgPostProcessImportAsset : AssetPostprocessor
    {
        public void OnPreprocessAsset()
        {
            if (PostProcessImportAsset.ProcessingFile != assetPath) return;

            if (assetImporter is SVGImporter svgImporter)
            {
                var userData = JsonConvert.DeserializeObject<SvgImportUserData>(PostProcessImportAsset.Asset.UserData ?? string.Empty);
                svgImporter.SvgType = SVGType.TexturedSprite;
                svgImporter.KeepTextureAspectRatio = false;
                svgImporter.TextureWidth = userData.Width;
                svgImporter.TextureHeight = userData.Height;
            }
        }

        public class SvgImportUserData
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }
    }
}
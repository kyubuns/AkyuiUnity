using AkyuiUnity.Editor;
using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Generator;
using Newtonsoft.Json;
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEngine;

namespace AkyuiUnity.Xd
{
    public class SvgImportTrigger : IAkyuiImportTrigger
    {
        public void OnUnityPreprocessAsset(AssetImporter assetImporter, IAsset asset)
        {
            if (!(assetImporter is SVGImporter svgImporter)) return;

            var userData = JsonConvert.DeserializeObject<SvgImportUserData>(PostProcessImportAsset.Asset.UserData ?? string.Empty);
            svgImporter.SvgType = SVGType.TexturedSprite;
            svgImporter.KeepTextureAspectRatio = false;
            svgImporter.TextureWidth = userData.Width;
            svgImporter.TextureHeight = userData.Height;
        }

        public class SvgImportUserData
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public void OnPreprocessAsset(ref byte[] bytes, ref IAsset asset) { }
        public void OnPostprocessPrefab(ref GameObject prefab, ref GameObjectWithId[] meta) { }
        public void OnPostprocessAllAssets(string outputDirectoryPath) { }
        public Component SetOrCreateComponentValue(GameObject gameObject, TargetComponentGetter componentGetter, IComponent component,
            GameObject[] children, IAssetLoader assetLoader) => null;
        public void OnPostprocessComponent(GameObject gameObject, IComponent component) { }
    }
}
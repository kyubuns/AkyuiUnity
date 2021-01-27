using AkyuiUnity.Editor;
using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Generator;
using AkyuiUnity.Loader;
using Newtonsoft.Json;
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEngine;

namespace AkyuiUnity.Xd
{
    public class SvgImportTrigger : IAkyuiImportTrigger
    {
        private readonly float _saveScale;

        public SvgImportTrigger(float saveScale)
        {
            _saveScale = saveScale;
        }

        public void OnUnityPreprocessAsset(AssetImporter assetImporter, IAsset asset)
        {
            if (!(assetImporter is SVGImporter svgImporter)) return;

            var userData = JsonConvert.DeserializeObject<SvgImportUserData>(PostProcessImportAsset.Asset.UserData ?? string.Empty);
            svgImporter.SvgType = SVGType.TexturedSprite;
            svgImporter.KeepTextureAspectRatio = false;
            svgImporter.TextureWidth = Mathf.RoundToInt(userData.Width * _saveScale);
            svgImporter.TextureHeight = Mathf.RoundToInt(userData.Height * _saveScale);
        }

        public class SvgImportUserData
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public void OnPreprocessAsset(IAkyuiLoader loader, ref byte[] bytes, ref IAsset asset) { }
        public void OnPostprocessPrefab(IAkyuiLoader loader, ref GameObject prefab) { }
        public void OnPostprocessAllAssets(IAkyuiLoader loader, string outputDirectoryPath, Object[] importAssets) { }
        public Component CreateComponent(GameObject gameObject, IComponent component, IAssetLoader assetLoader) => null;
        public void OnPostprocessComponent(GameObject gameObject, IComponent component) { }
    }
}
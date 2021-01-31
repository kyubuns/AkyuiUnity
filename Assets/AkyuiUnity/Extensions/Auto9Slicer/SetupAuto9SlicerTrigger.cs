#if AKYUIUNITY_AUTO9SLICER_SUPPORT
using System.Collections.Generic;
using System.IO;
using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Loader;
using Auto9Slicer;
using UnityEngine;

namespace AkyuiUnity.AnKuchenExtension
{
    [CreateAssetMenu(menuName = "Akyui/Triggers/SetupAuto9Slicer", fileName = nameof(SetupAuto9SlicerTrigger))]
    public class SetupAuto9SlicerTrigger : AkyuiImportTrigger
    {
        [SerializeField] private SliceOptions options;

        public override void OnPreprocessAsset(IAkyuiLoader loader, ref byte[] bytes, ref IAsset asset, ref Dictionary<string, object> userData)
        {
            if (!(asset is SpriteAsset spriteAsset)) return;

            var extension = Path.GetExtension(asset.FileName);
            if (spriteAsset.Border != null) return;
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg") return;

            var texture = new Texture2D(2, 2);
            texture.LoadImage(bytes);

            var sliced = Slicer.Slice(texture, options);
            if (extension == ".png") bytes = sliced.Texture.EncodeToPNG();
            if (extension == ".jpg" || extension == ".jpeg") bytes = sliced.Texture.EncodeToJPG();

            spriteAsset.Border = new Border(
                sliced.Border.Top,
                sliced.Border.Right,
                sliced.Border.Bottom,
                sliced.Border.Left
            );
        }
    }
}
#endif

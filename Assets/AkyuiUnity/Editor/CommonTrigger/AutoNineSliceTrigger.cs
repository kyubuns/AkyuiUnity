using System.Collections.Generic;
using System.IO;
using AkyuiUnity.CommonTrigger.Library.OnionRing;
using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Loader;
using UnityEngine;

namespace AkyuiUnity.CommonTrigger
{
    [CreateAssetMenu(menuName = "Akyui/Triggers/AutoNineSlice", fileName = nameof(AutoNineSliceTrigger))]
    public class AutoNineSliceTrigger : AkyuiImportTrigger
    {
        public override void OnPreprocessAsset(IAkyuiLoader loader, ref byte[] bytes, ref IAsset asset, ref Dictionary<string, object> userData)
        {
            if (!(asset is SpriteAsset spriteAsset)) return;

            var extension = Path.GetExtension(asset.FileName);
            if (spriteAsset.Border != null) return;
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg") return;

            var texture = new Texture2D(2, 2);
            texture.LoadImage(bytes);

            var sliced = TextureSlicer.Slice(texture);
            if (extension == ".png") bytes = sliced.Texture.EncodeToPNG();
            if (extension == ".jpg" || extension == ".jpeg") bytes = sliced.Texture.EncodeToJPG();

            spriteAsset.Border = new Border(
                sliced.Boarder.Top,
                sliced.Boarder.Right,
                sliced.Boarder.Bottom,
                sliced.Boarder.Left
            );
        }
    }
}
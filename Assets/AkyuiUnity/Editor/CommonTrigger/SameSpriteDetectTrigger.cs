using System.Collections.Generic;
using System.Linq;
using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Loader;
using UnityEngine;

namespace AkyuiUnity.CommonTrigger
{
    [CreateAssetMenu(menuName = "Akyui/Triggers/SameSpriteDetect", fileName = nameof(SameSpriteDetectTrigger))]
    public class SameSpriteDetectTrigger : AkyuiImportTrigger
    {
        private Dictionary<string, string> _map;

        public override void OnPreprocessAllAssets(IAkyuiLoader akyuiLoader, ref List<IAsset> assets)
        {
            _map = new Dictionary<string, string>();

            var removed = new List<IAsset>();
            var cache = new Dictionary<ulong, IAsset>();

            foreach (var asset in assets)
            {
                if (!(asset is SpriteAsset spriteAsset)) continue;

                var loadAsset = akyuiLoader.LoadAsset(spriteAsset.FileName);
                var hash = FastHash.CalculateHash(loadAsset.Select(x => (uint) x).ToArray())
                           + (spriteAsset.Border == null ? FastHash.CalculateHash("NoBorder") : FastHash.CalculateHash($"Border{spriteAsset.Border.ToVector4()}"))
                    ;

                if (!cache.ContainsKey(hash))
                {
                    cache.Add(hash, asset);
                    continue;
                }

                var hit = cache[hash];
                removed.Add(asset);
                _map[asset.FileName] = hit.FileName;
            }

            foreach (var r in removed) assets.Remove(r);

            if (removed.Count > 0)
            {
                Debug.Log($"[SameSpriteDetect] Remove {removed.Count} assets");
            }
        }

        public override string OnLoadAsset(string fileName)
        {
            if (_map.ContainsKey(fileName)) return _map[fileName];
            return fileName;
        }
    }
}

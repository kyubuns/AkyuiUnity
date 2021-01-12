using System;
using System.Linq;
using UnityEngine;

namespace AkyuiUnity
{
    public class AkyuiMeta : MonoBehaviour
    {
        [SerializeField] public AkyuiPrefabMeta meta;

        public AkyuiPrefabMeta GetCopiedMeta()
        {
            return new AkyuiPrefabMeta
            {
                timestamp = meta.timestamp,
                root = meta.root,
                idAndGameObjects = meta.idAndGameObjects
            };
        }
    }

    [Serializable]
    public class AkyuiPrefabMeta
    {
        [SerializeField] public int timestamp;
        [SerializeField] public GameObject root;
        [SerializeField] public GameObjectWithId[] idAndGameObjects;

        public GameObjectWithId Find(int[] eid)
        {
            return idAndGameObjects.First(x =>
            {
                if (x.eid.Length != eid.Length) return false;
                for (var i = 0; i < eid.Length; ++i)
                {
                    if (x.eid[i] != eid[i]) return false;
                }
                return true;
            });
        }
    }

    [Serializable]
    public class GameObjectWithId
    {
        [SerializeField] public int[] eid;
        [SerializeField] public GameObject gameObject;
        [SerializeField] public ComponentWithId[] idAndComponents;
    }

    [Serializable]
    public class ComponentWithId
    {
        [SerializeField] public int cid;
        [SerializeField] public Component component;
    }
}
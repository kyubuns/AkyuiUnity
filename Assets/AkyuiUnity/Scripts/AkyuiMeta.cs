using System;
using System.Linq;
using UnityEngine;

namespace AkyuiUnity
{
    public class AkyuiMeta : MonoBehaviour
    {
        [SerializeField] public GameObject root;
        [SerializeField] public IdAndGameObject[] idAndGameObjects;

        public IdAndGameObject Find(int[] id)
        {
            return idAndGameObjects.First(x =>
            {
                if (x.id.Length != id.Length) return false;
                for (var i = 0; i < id.Length; ++i)
                {
                    if (x.id[i] != id[i]) return false;
                }
                return true;
            });
        }
    }

    [Serializable]
    public class IdAndGameObject
    {
        [SerializeField] public int[] id;
        [SerializeField] public GameObject gameObject;
        [SerializeField] public Component[] components;
    }
}
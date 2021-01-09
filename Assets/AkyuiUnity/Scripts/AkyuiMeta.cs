using System;
using System.Linq;
using UnityEngine;

namespace AkyuiUnity
{
    public class AkyuiMeta : MonoBehaviour
    {
        [SerializeField] public IdAndGameObject[] idAndGameObjects;

        public IdAndGameObject Find(int id)
        {
            return idAndGameObjects.First(x => x.id == id);
        }
    }

    [Serializable]
    public class IdAndGameObject
    {
        [SerializeField] public int id;
        [SerializeField] public GameObject gameObject;
        [SerializeField] public Component[] components;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AkyuiUnity
{
    public class AkyuiMeta : MonoBehaviour
    {
        [SerializeField] public long hash;
        [SerializeField] public GameObject root;
        [SerializeField] public Object[] assets;
        [SerializeField] public AkyuiMetaUserData[] userData;

        public AkyuiMetaUserData FindUserData(string key)
        {
            return userData?.FirstOrDefault(x => x.key == key);
        }
    }

    [Serializable]
    public class AkyuiMetaUserData
    {
        [SerializeField] public string key;
        [SerializeField] public string value;
    }
}
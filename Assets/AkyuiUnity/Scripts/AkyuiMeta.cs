using System;
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
    }
}
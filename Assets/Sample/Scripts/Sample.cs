using AnKuchen.Map;
using UnityEngine;
using UnityEngine.UI;

namespace AkyuiUnity.Sample
{
    public class Sample : MonoBehaviour
    {
        [SerializeField] private UICache uiCache = default;

        public void Start()
        {
            var ui = new UiElements(uiCache);
        }

        public class UiElements : IMappedObject
        {
            public IMapper Mapper { get; private set; }
            public GameObject Root { get; private set; }

            public UiElements()
            {
            }

            public UiElements(IMapper mapper)
            {
                Initialize(mapper);
            }

            public void Initialize(IMapper mapper)
            {
                Mapper = mapper;
                Root = mapper.Get();
            }
        }
    }
}

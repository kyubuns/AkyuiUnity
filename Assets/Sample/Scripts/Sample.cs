using AnKuchen.Extensions;
using AnKuchen.Layout;
using AnKuchen.Map;
using UnityEngine;

namespace AkyuiUnity.Sample
{
    public class Sample : MonoBehaviour
    {
        [SerializeField] private UICache uiCache = default;

        public void Start()
        {
            var ui = new UiElements(uiCache);
            for (var i = 0; i < 10; ++i)
            {
                ui.Dummy.Duplicate();
            }
        }

        private class UiElements : IMappedObject
        {
            public IMapper Mapper { get; private set; }
            public GameObject Root { get; private set; }
            public DummyUiElements Dummy { get; private set; }

            public UiElements(IMapper mapper)
            {
                Initialize(mapper);
            }

            public void Initialize(IMapper mapper)
            {
                Mapper = mapper;
                Root = mapper.Get();

                Dummy = mapper.GetChild<DummyUiElements>("Dummy");
            }
        }

        private class DummyUiElements : IMappedObject
        {
            public IMapper Mapper { get; private set; }
            public GameObject Root { get; private set; }

            public DummyUiElements() { }
            public DummyUiElements(IMapper mapper) { Initialize(mapper); }

            public void Initialize(IMapper mapper)
            {
                Mapper = mapper;
                Root = mapper.Get();
            }
        }

    }
}

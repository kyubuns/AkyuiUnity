using AnKuchen.Extensions;
using AnKuchen.Layout;
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

            for (var i = 0; i < 4; ++i)
            {
                ui.Button.Duplicate();
            }
        }

        public class UiElements : IMappedObject
        {
            public IMapper Mapper { get; private set; }
            public GameObject Root { get; private set; }
            public ButtonUiElements Button { get; private set; }

            public UiElements() { }
            public UiElements(IMapper mapper) { Initialize(mapper); }

            public void Initialize(IMapper mapper)
            {
                Mapper = mapper;
                Root = mapper.Get();
                Button = mapper.GetChild<ButtonUiElements>("Button");
            }
        }

        public class ButtonUiElements : IMappedObject
        {
            public IMapper Mapper { get; private set; }
            public GameObject Root { get; private set; }
            public Image ButtonBase { get; private set; }
            public Text ButtonText { get; private set; }

            public ButtonUiElements() { }
            public ButtonUiElements(IMapper mapper) { Initialize(mapper); }

            public void Initialize(IMapper mapper)
            {
                Mapper = mapper;
                Root = mapper.Get();
                ButtonBase = mapper.Get<Image>("ButtonBase");
                ButtonText = mapper.Get<Text>("ButtonText");
            }
        }
    }
}

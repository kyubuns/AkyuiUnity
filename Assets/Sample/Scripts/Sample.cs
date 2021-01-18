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
            ui.ButtonGroup.onClick.AddListener(() => Debug.Log("Click!"));
        }

        public class UiElements : IMappedObject
        {
            public IMapper Mapper { get; private set; }
            public GameObject Root { get; private set; }
            public Image Background { get; private set; }
            public Image Avocado { get; private set; }
            public Button ButtonGroup { get; private set; }
            public Image ButtonBase { get; private set; }
            public Text ButtonText { get; private set; }

            public UiElements() { }
            public UiElements(IMapper mapper) { Initialize(mapper); }

            public void Initialize(IMapper mapper)
            {
                Mapper = mapper;
                Root = mapper.Get();
                Background = mapper.Get<Image>("Background");
                Avocado = mapper.Get<Image>("avocado");
                ButtonGroup = mapper.Get<Button>("ButtonGroup");
                ButtonBase = mapper.Get<Image>("ButtonBase");
                ButtonText = mapper.Get<Text>("ButtonText");
            }
        }


    }
}

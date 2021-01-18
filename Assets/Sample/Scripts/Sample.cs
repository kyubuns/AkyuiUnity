using AnKuchen.KuchenList;
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

            using (var editor = ui.List.Edit())
            {
                for (var i = 0; i < 5; ++i)
                {
                    var i1 = i;
                    editor.Contents.Add(new UIFactory<ButtonUiElements>(x =>
                    {
                        x.ButtonText.text = $"Button{i1}";
                    }));
                }
            }
        }

        public class UiElements : IMappedObject
        {
            public IMapper Mapper { get; private set; }
            public GameObject Root { get; private set; }
            public ScrollRect ScrollRect { get; private set; }
            public VerticalList<ButtonUiElements> List { get; private set; }

            public UiElements() { }
            public UiElements(IMapper mapper) { Initialize(mapper); }

            public void Initialize(IMapper mapper)
            {
                Mapper = mapper;
                Root = mapper.Get();
                ScrollRect = mapper.Get<ScrollRect>("Scroll Group 1");
                List = new VerticalList<ButtonUiElements>(
                    ScrollRect,
                    mapper.GetChild<ButtonUiElements>("Button")
                );
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

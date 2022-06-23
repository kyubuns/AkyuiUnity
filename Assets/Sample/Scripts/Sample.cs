using AnKuchen.KuchenLayout;
using AnKuchen.KuchenLayout.Layouter;
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
                for (var y = 0; y < 5; ++y)
                {
                    editor.Contents.Add(new UIFactory<Items, Title>((Title x) =>
                    {
                    }));

                    for (var i = 0; i < 5; ++i)
                    {
                        editor.Contents.Add(new UIFactory<Items, Title>(x =>
                        {
                            using (var e = x.Row.Edit())
                            {
                                e.Create();
                                e.Create();
                                e.Create();
                            }
                        }));
                    }
                }
            }
        }
    }

    public class UiElements : IMappedObject
    {
        public IMapper Mapper { get; private set; }
        public GameObject Root { get; private set; }
        public VerticalList<Items, Title> List { get; private set; }

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
            mapper.Get<ScrollRect>("Scroll").verticalScrollbar = mapper.Get<Scrollbar>("TestScrollbar");
            List = new VerticalList<Items, Title>(
                mapper.Get<ScrollRect>("Scroll"),
                mapper.GetChild<Items>("Row"),
                mapper.GetChild<Title>("Title")
            );
        }
    }

    public class Title : IMappedObject
    {
        public IMapper Mapper { get; private set; }
        public GameObject Root { get; private set; }

        public Title()
        {
        }

        public Title(IMapper mapper)
        {
            Initialize(mapper);
        }

        public void Initialize(IMapper mapper)
        {
            Mapper = mapper;
            Root = mapper.Get();
        }
    }

    public class Items : IMappedObject
    {
        public IMapper Mapper { get; private set; }
        public GameObject Root { get; private set; }
        public Layout<Item> Row { get; private set; }

        public Items()
        {
        }

        public Items(IMapper mapper)
        {
            Initialize(mapper);
        }

        public void Initialize(IMapper mapper)
        {
            Mapper = mapper;
            Root = mapper.Get();
            Row = new Layout<Item>(
                mapper.GetChild<Item>("Item"),
                new LeftToRightLayouter(mapper.Get<HorizontalLayoutGroup>())
            );
        }
    }

    public class Item : IMappedObject
    {
        public IMapper Mapper { get; private set; }
        public GameObject Root { get; private set; }

        public Item()
        {
        }

        public Item(IMapper mapper)
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
